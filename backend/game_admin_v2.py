"""FastAPI 路由层 —— 不包含业务逻辑，只做参数校验和响应组装"""
import json
import random
from datetime import datetime
from typing import List, Optional

from fastapi import FastAPI, HTTPException, Depends, status
from sqlalchemy.orm import Session

from database import get_database_session
from model_class import (
    User,
    Level,
    LevelPart,
    UserLevelProgress,
    UserCreate,
    UserOut,
    LevelDetailOut,
    PartSubmitRequest,
    PartSubmitResponse,
    LevelSubmitRequest,
    LevelSubmitResponse,
)
from service import (
    get_user_game_status,
    create_and_init_user,
    get_level_full_details,
    unlock_next_level,
    unlock_first_level,
    PART_VALIDATORS,
)

app = FastAPI(
    title="LLM Puzzle Game Backend",
    version="2.0.0",
    description="Educational Puzzle Game — Level 1: Text Digitization (3 Parts)",
)


# ============================================================
#  通用
# ============================================================


@app.get("/", tags=["General"])
def check_server_status():
    return {
        "status": "online",
        "message": "LLM Puzzle Game — 文本数字化之旅",
    }


# ============================================================
#  1. 登录 / 注册（查询用户 → 无则创建 → 返回身份 + 简况）
# ============================================================


@app.post("/login", tags=["Game Flow"])
def player_login(username: str, db: Session = Depends(get_database_session)):
    user = db.query(User).filter(User.name == username).first()

    if user:
        message = "读取存档成功"
    else:
        user = create_and_init_user(db, username)
        message = "新账号创建成功，Level 1 已解锁"

    level_progress = get_user_game_status(db, user.id)

    return {
        "status": "success",
        "message": message,
        "user_data": {
            "id": user.id,
            "name": user.name,
            "cur_level_id": user.cur_level_id,
            "total_score": user.total_score,
        },
        "level_progress": level_progress,
    }


# ============================================================
#  2. 关卡详情（含 Parts 列表 + 可选用户进度）
# ============================================================


@app.get("/level/{level_id}", response_model=LevelDetailOut, tags=["Game Flow"])
def fetch_level_detail(
    level_id: int,
    user_id: Optional[int] = None,
    db: Session = Depends(get_database_session),
):
    result = get_level_full_details(db, level_id, user_id)
    if not result:
        raise HTTPException(status_code=404, detail=f"Level {level_id} 不存在")
    return result


# ============================================================
#  3. 获取某个 Part 的初始数据
# ============================================================


@app.get("/level/{level_id}/part/{part_order}", tags=["Game Flow"])
def fetch_part_data(
    level_id: int,
    part_order: int,
    user_id: int,
    db: Session = Depends(get_database_session),
):
    level = db.query(Level).filter(Level.id == level_id).first()
    if not level:
        raise HTTPException(status_code=404, detail="Level 不存在")

    part = (
        db.query(LevelPart)
        .filter(LevelPart.level_id == level_id, LevelPart.order == part_order)
        .first()
    )
    if not part:
        raise HTTPException(status_code=404, detail=f"Part {part_order} 不存在")

    progress = (
        db.query(UserLevelProgress)
        .filter(
            UserLevelProgress.user_id == user_id,
            UserLevelProgress.level_id == level_id,
        )
        .first()
    )
    if not progress or not progress.is_unlocked:
        raise HTTPException(status_code=403, detail="关卡未解锁")

    part_config = _json_load(part.config)
    part_data_obj = _json_load(progress.part_data)
    stored = part_data_obj.get(str(part_order), {})

    if part_order == 1:
        templates = part_config.get("prompt_templates", [])
        prompt = (
            random.choice(templates) if templates else "The model learns from data."
        )
        return {
            "part_order": 1,
            "title": part.title,
            "description": part.description,
            "prompt": stored.get("prompt", prompt),
            "hint": "请删去无关信息，将其拆分为子词 Token。使用左侧分词标识填入合适的空隙。",
            "stored_data": stored,
        }

    if part_order == 2:
        p1_data = part_data_obj.get("1", {})
        tokens = p1_data.get("tokens", []) if isinstance(p1_data, dict) else []
        if not tokens:
            tokens = ["The", "quick", "brown", "fox", "jumps"]

        vocab = {tok: 100 + i * 10 for i, tok in enumerate(tokens)}
        distractor = {"slow": 50, "cat": 60, "river": 70, "learn": 80, "data": 90}

        return {
            "part_order": 2,
            "title": part.title,
            "description": part.description,
            "rounds": part_config.get("rounds", 3),
            "current_round": stored.get("current_round", 1),
            "tokens_to_map": tokens,
            "vocabulary": {**vocab, **distractor},
            "hint": "将每个 Token 与词表中的 ID 一一对应，然后排序为正确的整数序列。",
            "stored_data": stored,
        }

    if part_order == 3:
        p1_data = part_data_obj.get("1", {})
        tokens = p1_data.get("tokens", []) if isinstance(p1_data, dict) else []
        if not tokens:
            tokens = ["The", "quick", "brown", "fox", "jumps"]
        return {
            "part_order": 3,
            "title": part.title,
            "description": part.description,
            "tokens": tokens,
            "hint": "将 Token 拖入 3D 坐标系，词义相近的 Token 会互相闪烁靠近。调整视角观察向量空间。",
            "stored_data": stored,
        }

    return {"part_order": part_order, "title": part.title, "stored_data": stored}


# ============================================================
#  4. Part 提交
# ============================================================


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
    # ---------- 校验 ----------
    db_user = db.query(User).filter(User.id == data.player_id).first()
    if not db_user:
        raise HTTPException(status_code=404, detail="未找到该玩家")

    level = db.query(Level).filter(Level.id == level_id).first()
    if not level:
        raise HTTPException(status_code=404, detail="关卡不存在")

    part = (
        db.query(LevelPart)
        .filter(LevelPart.level_id == level_id, LevelPart.order == part_order)
        .first()
    )
    if not part:
        raise HTTPException(status_code=404, detail=f"Part {part_order} 不存在")

    progress = (
        db.query(UserLevelProgress)
        .filter(
            UserLevelProgress.user_id == data.player_id,
            UserLevelProgress.level_id == level_id,
        )
        .first()
    )
    if not progress or not progress.is_unlocked:
        raise HTTPException(status_code=403, detail="关卡未解锁")

    if part_order > progress.current_part and not progress.is_completed:
        raise HTTPException(
            status_code=400, detail=f"请先完成 Part {progress.current_part}"
        )

    # ---------- 校验答案 ----------
    part_config = _json_load(part.config)
    validator = PART_VALIDATORS.get(part_order)
    is_correct = validator(part_config, data.answer) if validator else True

    # ---------- 更新 part_data ----------
    part_data_obj = _json_load(progress.part_data)

    submission_entry = {
        "submitted_at": datetime.now().isoformat(),
        "answer": data.answer,
        "is_correct": is_correct,
        "score_earned": data.score_earned if is_correct else 0,
    }

    existing = part_data_obj.get(str(part_order), {})
    if isinstance(existing, dict):
        attempts = existing.get("attempts", [])
        attempts.append(submission_entry)
        existing["attempts"] = attempts
        existing["last_submission"] = submission_entry
        if is_correct:
            existing["completed"] = True
            existing["final_answer"] = data.answer
        part_data_obj[str(part_order)] = existing
    else:
        part_data_obj[str(part_order)] = {
            "attempts": [submission_entry],
            "last_submission": submission_entry,
        }

    progress.part_data = json.dumps(part_data_obj, ensure_ascii=False)
    progress.attempts += 1

    # ---------- 进度推进 ----------
    is_part_completed = is_correct
    is_level_completed = False
    next_level_id = None

    if is_correct:
        progress.score += data.score_earned
        db_user.total_score += data.score_earned

        total_parts = (
            db.query(LevelPart).filter(LevelPart.level_id == level_id).count()
        )
        completed_parts = sum(
            1
            for i in range(1, total_parts + 1)
            if isinstance(part_data_obj.get(str(i)), dict)
            and part_data_obj[str(i)].get("last_submission", {}).get("is_correct")
        )

        if completed_parts >= total_parts:
            progress.is_completed = True
            progress.completed_at = datetime.now().isoformat()
            if level_id >= db_user.cur_level_id:
                db_user.cur_level_id = level_id + 1
            is_level_completed = True
            next_level_id = unlock_next_level(data.player_id, level_id, db)
        else:
            if part_order + 1 <= total_parts:
                progress.current_part = part_order + 1

    db.commit()
    db.refresh(db_user)

    return PartSubmitResponse(
        message="Part 完成！" if is_correct else "不对，再试试",
        is_correct=is_correct,
        is_part_completed=is_part_completed,
        is_level_completed=is_level_completed,
        current_part=progress.current_part,
        current_total_score=db_user.total_score,
        next_level_unlocked=next_level_id,
    )


# ============================================================
#  5. 用户进度总览
# ============================================================


@app.get("/users/{user_id}/progress", tags=["Game Flow"])
def fetch_user_progress(user_id: int, db: Session = Depends(get_database_session)):
    user = db.query(User).filter(User.id == user_id).first()
    if not user:
        raise HTTPException(status_code=404, detail="未找到该玩家")

    progresses = (
        db.query(UserLevelProgress)
        .filter(UserLevelProgress.user_id == user_id)
        .all()
    )

    results = []
    for p in progresses:
        lvl = db.query(Level).filter(Level.id == p.level_id).first()
        part_data_obj = _json_load(p.part_data)
        total_parts = (
            db.query(LevelPart).filter(LevelPart.level_id == p.level_id).count()
        )

        parts_status = []
        for i in range(1, total_parts + 1):
            pd = part_data_obj.get(str(i), {})
            completed = (
                isinstance(pd, dict)
                and (
                    pd.get("last_submission", {}).get("is_correct", False)
                    or pd.get("completed", False)
                )
            )
            parts_status.append({"part": i, "completed": completed})

        results.append(
            {
                "level_id": p.level_id,
                "title": lvl.title if lvl else f"Level {p.level_id}",
                "description": lvl.description if lvl else "",
                "is_unlocked": p.is_unlocked,
                "is_completed": p.is_completed,
                "current_part": p.current_part,
                "score": p.score,
                "attempts": p.attempts,
                "completed_at": p.completed_at,
                "parts_status": parts_status,
                "level_data": _json_load(p.level_data) if p.level_data else None,
            }
        )

    return {
        "user_id": user.id,
        "username": user.name,
        "total_score": user.total_score,
        "current_level_id": user.cur_level_id,
        "progress": sorted(results, key=lambda x: x["level_id"]),
    }


# ============================================================
#  6. 关卡管理
# ============================================================


@app.get("/levels", tags=["Level Management"])
def fetch_all_levels(db: Session = Depends(get_database_session)):
    levels = db.query(Level).all()
    result = []
    for lvl in levels:
        parts = (
            db.query(LevelPart)
            .filter(LevelPart.level_id == lvl.id)
            .order_by(LevelPart.order)
            .all()
        )
        result.append(
            {
                "id": lvl.id,
                "title": lvl.title,
                "description": lvl.description,
                "parts_count": len(parts),
                "parts": [{"order": p.order, "title": p.title} for p in parts],
            }
        )
    return result


# ============================================================
#  7. 用户管理 CRUD
# ============================================================


@app.get("/users/", response_model=List[UserOut], tags=["User Management"])
def fetch_all_users(
    skip: int = 0, limit: int = 100, db: Session = Depends(get_database_session)
):
    return db.query(User).offset(skip).limit(limit).all()


@app.get("/users/{user_id}", response_model=UserOut, tags=["User Management"])
def fetch_user_by_id(user_id: int, db: Session = Depends(get_database_session)):
    user = db.query(User).filter(User.id == user_id).first()
    if not user:
        raise HTTPException(status_code=404, detail=f"User {user_id} not found")
    return user


@app.post(
    "/users/",
    response_model=UserOut,
    status_code=status.HTTP_201_CREATED,
    tags=["User Management"],
)
def register_new_user(
    user_in: UserCreate, db: Session = Depends(get_database_session)
):
    if db.query(User).filter(User.name == user_in.name).first():
        raise HTTPException(status_code=400, detail="该用户名已存在")
    new_user = User(**user_in.model_dump())
    db.add(new_user)
    db.commit()
    db.refresh(new_user)
    unlock_first_level(new_user.id, db)
    db.commit()
    return new_user


@app.put("/users/{user_id}", response_model=UserOut, tags=["User Management"])
def modify_user_profile(
    user_id: int, user_update: UserCreate, db: Session = Depends(get_database_session)
):
    db_user = db.query(User).filter(User.id == user_id).first()
    if not db_user:
        raise HTTPException(status_code=404, detail="User not found")
    for key, value in user_update.model_dump().items():
        setattr(db_user, key, value)
    db.commit()
    db.refresh(db_user)
    return db_user


@app.delete(
    "/users/{user_id}", status_code=status.HTTP_204_NO_CONTENT, tags=["User Management"]
)
def remove_user_record(user_id: int, db: Session = Depends(get_database_session)):
    db_user = db.query(User).filter(User.id == user_id).first()
    if not db_user:
        raise HTTPException(status_code=404, detail="User not found")
    db.delete(db_user)
    db.commit()
    return None


# ============================================================
#  8. 开发辅助：填充初始关卡数据
# ============================================================


@app.post("/seed/level1", tags=["Development"])
def seed_level1(db: Session = Depends(get_database_session)):
    if db.query(Level).filter(Level.id == 1).first():
        return {"message": "Level 1 已存在，跳过填充"}

    level = Level(
        id=1,
        title="Text Digitization (文本数字化)",
        description="通过 Tokenization → Token ID Mapping → Embedding 三步理解 LLM 如何处理文本。",
        config=json.dumps({"total_parts": 3}, ensure_ascii=False),
    )
    db.add(level)
    db.flush()

    parts = [
        LevelPart(
            level_id=level.id,
            order=1,
            title="Tokenization",
            description="将 AI 生成的 prompt 拆分为子词 Token。删去无关信息，用分词标识填入空隙。",
            config=json.dumps(
                {
                    "prompt_templates": [
                        "The quick brown fox jumps over the lazy dog near the riverbank.",
                        "Artificial intelligence models learn from vast amounts of text data every day.",
                        "Transformer architecture revolutionized natural language processing tasks.",
                        "Neural networks process information through layers of connected nodes.",
                        "Attention mechanisms help models focus on important parts of the input sequence.",
                    ],
                    "expected_tokens": None,
                },
                ensure_ascii=False,
            ),
        ),
        LevelPart(
            level_id=level.id,
            order=2,
            title="Token ID Mapping",
            description="将 Token 映射为数字 ID，排序为正确整数序列。重复 3 轮。",
            config=json.dumps({"rounds": 3}, ensure_ascii=False),
        ),
        LevelPart(
            level_id=level.id,
            order=3,
            title="Vector Representation (Embedding)",
            description="将 Token 序列放入 3D 向量空间，词义相近的 Token 互相靠近闪烁。",
            config=json.dumps({}, ensure_ascii=False),
        ),
    ]
    db.add_all(parts)
    db.commit()
    return {"message": "Level 1 填充完成（3 个 Parts）"}


# ============================================================
#  工具
# ============================================================


def _json_load(raw: Optional[str]) -> dict:
    if not raw:
        return {}
    try:
        return json.loads(raw)
    except (json.JSONDecodeError, TypeError):
        return {}
