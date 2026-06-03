from datetime import datetime
from typing import List, Optional

from fastapi import Depends, FastAPI, HTTPException, status
from sqlalchemy.orm import Session

from database import get_database_session
from model_class import (
    Level,
    LevelDetailOut,
    LevelPart,
    LevelSubmitRequest,
    LevelSubmitResponse,
    PartSubmitRequest,
    PartSubmitResponse,
    User,
    UserCreate,
    UserLevelProgress,
    UserOut,
)
from service import (
    complete_level,
    create_and_init_user,
    get_level_full_details,
    get_level_lock_reason,
    get_user_game_status,
    parse_json,
    seed_levels,
    to_json,
    unlock_next_level,
    validate_answer,
)


app = FastAPI(
    title="LLM Puzzle Game Backend",
    version="3.0.0",
    description="Progress backend for the Unity LLM puzzle game.",
)


@app.on_event("startup")
def seed_on_startup():
    from database import init_database

    init_database()
    db = next(get_database_session())
    try:
        seed_levels(db)
    finally:
        db.close()


@app.get("/", tags=["General"])
def check_server_status():
    return {"status": "online", "message": "LLM Puzzle Game backend is running"}


@app.get("/health", tags=["General"])
def health_check():
    return {"ok": True}


@app.post("/seed", tags=["Development"])
def seed_all_levels(db: Session = Depends(get_database_session)):
    seed_levels(db)
    return {"message": "Seeded levels 1-4"}


@app.post("/login", tags=["Game Flow"])
def player_login(username: str, db: Session = Depends(get_database_session)):
    clean_username = username.strip()
    if not clean_username:
        raise HTTPException(status_code=400, detail="username is required")

    user = db.query(User).filter(User.name == clean_username).first()
    if user is None:
        user = create_and_init_user(db, clean_username)
        message = "New player created"
    else:
        message = "Loaded existing player"

    return {
        "status": "success",
        "message": message,
        "user_data": {
            "id": user.id,
            "name": user.name,
            "cur_level_id": user.cur_level_id,
            "total_score": user.total_score,
        },
        "level_progress": get_user_game_status(db, user.id),
    }


@app.get("/level/{level_id}", response_model=LevelDetailOut, tags=["Game Flow"])
def fetch_level_detail(
    level_id: int,
    user_id: Optional[int] = None,
    db: Session = Depends(get_database_session),
):
    result = get_level_full_details(db, level_id, user_id)
    if result is None:
        raise HTTPException(status_code=404, detail=f"Level {level_id} not found")
    return result


@app.get("/level/{level_id}/part/{part_order}", tags=["Game Flow"])
def fetch_part_data(
    level_id: int,
    part_order: int,
    user_id: int,
    db: Session = Depends(get_database_session),
):
    part = (
        db.query(LevelPart)
        .filter(LevelPart.level_id == level_id, LevelPart.order == part_order)
        .first()
    )
    if part is None:
        raise HTTPException(status_code=404, detail="Part not found")

    progress = (
        db.query(UserLevelProgress)
        .filter(
            UserLevelProgress.user_id == user_id,
            UserLevelProgress.level_id == level_id,
        )
        .first()
    )
    if progress is None or not progress.is_unlocked:
        raise HTTPException(status_code=403, detail="Level is locked")

    return {
        "level_id": level_id,
        "part_order": part_order,
        "title": part.title,
        "description": part.description,
        "config": parse_json(part.config),
        "stored_data": parse_json(progress.part_data).get(str(part_order), {}),
    }


@app.post(
    "/level/{level_id}/part/{part_order}/submit",
    response_model=PartSubmitResponse,
    tags=["Game Flow"],
)
def submit_part(
    level_id: int,
    part_order: int,
    data: PartSubmitRequest,
    db: Session = Depends(get_database_session),
):
    if data.level_id != level_id or data.part_order != part_order:
        raise HTTPException(status_code=400, detail="Path and request body do not match")

    user = db.query(User).filter(User.id == data.player_id).first()
    if user is None:
        raise HTTPException(status_code=404, detail="Player not found")

    part = (
        db.query(LevelPart)
        .filter(LevelPart.level_id == level_id, LevelPart.order == part_order)
        .first()
    )
    if part is None:
        raise HTTPException(status_code=404, detail="Part not found")

    progress = (
        db.query(UserLevelProgress)
        .filter(
            UserLevelProgress.user_id == data.player_id,
            UserLevelProgress.level_id == level_id,
        )
        .first()
    )
    if progress is None or not progress.is_unlocked:
        raise HTTPException(status_code=403, detail="Level is locked")

    is_correct = validate_answer(parse_json(part.config), data.answer)
    part_data = parse_json(progress.part_data)
    entry = {
        "submitted_at": datetime.now().isoformat(),
        "answer": data.answer,
        "is_correct": is_correct,
        "score_earned": data.score_earned if is_correct else 0,
    }

    current = part_data.get(str(part_order), {})
    attempts = current.get("attempts", []) if isinstance(current, dict) else []
    attempts.append(entry)
    part_data[str(part_order)] = {
        "attempts": attempts,
        "last_submission": entry,
        "completed": is_correct or current.get("completed", False),
        "final_answer": data.answer if is_correct else current.get("final_answer"),
    }
    progress.part_data = to_json(part_data)
    progress.attempts += 1

    is_level_completed = False
    next_level_id = None

    if is_correct:
        progress.score += data.score_earned
        user.total_score += data.score_earned
        total_parts = db.query(LevelPart).filter(LevelPart.level_id == level_id).count()
        completed_parts = sum(
            1
            for idx in range(1, total_parts + 1)
            if part_data.get(str(idx), {}).get("completed", False)
        )

        if completed_parts >= total_parts:
            progress.is_completed = True
            progress.completed_at = datetime.now().isoformat()
            is_level_completed = True
            if level_id >= user.cur_level_id:
                user.cur_level_id = level_id + 1
            next_level_id = unlock_next_level(data.player_id, level_id, db)
        else:
            progress.current_part = min(part_order + 1, total_parts)

    db.commit()
    db.refresh(user)

    return PartSubmitResponse(
        message="Part completed" if is_correct else "Incorrect answer",
        is_correct=is_correct,
        is_part_completed=is_correct,
        is_level_completed=is_level_completed,
        current_part=progress.current_part,
        current_total_score=user.total_score,
        next_level_unlocked=next_level_id,
    )


@app.post(
    "/level/{level_id}/complete",
    response_model=LevelSubmitResponse,
    tags=["Game Flow"],
)
def submit_level_complete(
    level_id: int,
    data: LevelSubmitRequest,
    db: Session = Depends(get_database_session),
):
    if data.level_id != level_id:
        raise HTTPException(status_code=400, detail="Path and request body do not match")

    lock_reason = get_level_lock_reason(db, data.player_id, level_id)
    if lock_reason is not None:
        raise HTTPException(status_code=403, detail=lock_reason)

    user, progress, next_level_id = complete_level(
        db,
        user_id=data.player_id,
        level_id=level_id,
        score_earned=data.score_earned,
        custom_data=data.custom_data,
    )
    if user is None or progress is None:
        raise HTTPException(status_code=404, detail="Player or level not found")

    return LevelSubmitResponse(
        message=f"Level {level_id} completed",
        current_total_score=user.total_score,
        is_level_completed=True,
        next_level_unlocked=next_level_id,
    )


@app.get("/users/{user_id}/progress", tags=["Game Flow"])
def fetch_user_progress(user_id: int, db: Session = Depends(get_database_session)):
    user = db.query(User).filter(User.id == user_id).first()
    if user is None:
        raise HTTPException(status_code=404, detail="Player not found")

    return {
        "user_id": user.id,
        "username": user.name,
        "total_score": user.total_score,
        "current_level_id": user.cur_level_id,
        "progress": get_user_game_status(db, user.id),
    }


@app.get("/levels", tags=["Level Management"])
def fetch_all_levels(db: Session = Depends(get_database_session)):
    levels = db.query(Level).order_by(Level.id).all()
    return [
        {
            "id": level.id,
            "title": level.title,
            "description": level.description,
            "config": parse_json(level.config),
            "parts_count": len(level.parts),
            "parts": [
                {"order": part.order, "title": part.title}
                for part in sorted(level.parts, key=lambda p: p.order)
            ],
        }
        for level in levels
    ]


@app.get("/users/", response_model=List[UserOut], tags=["User Management"])
def fetch_all_users(
    skip: int = 0, limit: int = 100, db: Session = Depends(get_database_session)
):
    return db.query(User).offset(skip).limit(limit).all()


@app.get("/users/{user_id}", response_model=UserOut, tags=["User Management"])
def fetch_user_by_id(user_id: int, db: Session = Depends(get_database_session)):
    user = db.query(User).filter(User.id == user_id).first()
    if user is None:
        raise HTTPException(status_code=404, detail=f"User {user_id} not found")
    return user


@app.post(
    "/users/",
    response_model=UserOut,
    status_code=status.HTTP_201_CREATED,
    tags=["User Management"],
)
def register_new_user(user_in: UserCreate, db: Session = Depends(get_database_session)):
    if db.query(User).filter(User.name == user_in.name).first() is not None:
        raise HTTPException(status_code=400, detail="Username already exists")

    user = User(**user_in.model_dump())
    db.add(user)
    db.commit()
    db.refresh(user)
    get_user_game_status(db, user.id)
    return user
