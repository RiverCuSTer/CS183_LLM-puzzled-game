# Responsible team member: Jiayu Guo, Zhiyan Lin; Description: Provides backend service logic for seeding levels, user progress, answer validation, and unlock handling.
import json
from datetime import datetime
from typing import Optional

from sqlalchemy.orm import Session

from model_class import Level, LevelPart, User, UserLevelProgress


LEVEL_SEED_DATA = [
    {
        "id": 1,
        "title": "Text Digitization",
        "description": "Tokenization, token ID mapping, and embedding.",
        "config": {"total_parts": 3, "scene": "Level1"},
        "parts": [
            {
                "order": 1,
                "title": "Tokenization",
                "description": "Split a sentence into token boundaries.",
                "config": {
                    "sentences": [
                        "Move the red block",
                        "Open the blue door",
                        "Find a hidden key",
                    ]
                },
            },
            {
                "order": 2,
                "title": "Token ID Mapping",
                "description": "Map each token to its vocabulary ID.",
                "config": {"rounds": 3},
            },
            {
                "order": 3,
                "title": "Embedding Space",
                "description": "Place tokens in semantic regions.",
                "config": {},
            },
        ],
    },
    {
        "id": 2,
        "title": "Self-Attention",
        "description": "Build attention connections and normalize their weights.",
        "config": {"total_parts": 3, "scene": "Level2"},
        "parts": [
            {
                "order": 1,
                "title": "Symbol Placement",
                "description": "Fill the token slots before attention setup.",
                "config": {},
            },
            {
                "order": 2,
                "title": "Attention Weights",
                "description": "Adjust attention lines to the correct strength.",
                "config": {"nodes": ["Who", "Am", "I"]},
            },
            {
                "order": 3,
                "title": "Normalization",
                "description": "Convert raw attention weights into normalized values.",
                "config": {},
            },
        ],
    },
    {
        "id": 3,
        "title": "Semantic Structure",
        "description": "Connect word relations and stack semantic layers.",
        "config": {"total_parts": 4, "scene": "Level3"},
        "parts": [
            {
                "order": 1,
                "title": "Word Relations",
                "description": "Connect related words.",
                "config": {},
            },
            {
                "order": 2,
                "title": "Syntax Towers",
                "description": "Drag physical word blocks into syntax towers.",
                "config": {},
            },
            {
                "order": 3,
                "title": "Semantic Scene",
                "description": "Complete the semantic layer.",
                "config": {},
            },
            {
                "order": 4,
                "title": "Final Layer",
                "description": "Finish the last semantic structure layer.",
                "config": {},
            },
        ],
    },
    {
        "id": 4,
        "title": "Feed-Forward Network",
        "description": "Expand, activate, filter, and compress token features.",
        "config": {"total_parts": 1, "scene": "Level4"},
        "parts": [
            {
                "order": 1,
                "title": "FFN Feature Filtering",
                "description": "Apply intensify, refrain, or save, then place output features.",
                "config": {},
            }
        ],
    },
]


def to_json(data) -> str:
    return json.dumps(data, ensure_ascii=False)


def parse_json(raw: Optional[str]):
    if not raw:
        return {}
    try:
        return json.loads(raw)
    except (json.JSONDecodeError, TypeError):
        return {}


def seed_levels(db: Session):
    for item in LEVEL_SEED_DATA:
        level = db.query(Level).filter(Level.id == item["id"]).first()
        if level is None:
            level = Level(id=item["id"])
            db.add(level)

        level.title = item["title"]
        level.description = item["description"]
        level.config = to_json(item["config"])
        db.flush()

        existing_parts = {
            part.order: part
            for part in db.query(LevelPart).filter(LevelPart.level_id == level.id).all()
        }

        for part_item in item["parts"]:
            part = existing_parts.get(part_item["order"])
            if part is None:
                part = LevelPart(level_id=level.id, order=part_item["order"])
                db.add(part)

            part.title = part_item["title"]
            part.description = part_item["description"]
            part.config = to_json(part_item["config"])

    db.commit()


def create_and_init_user(db: Session, username: str):
    user = User(name=username, cur_level_id=1, total_score=0)
    db.add(user)
    db.commit()
    db.refresh(user)
    ensure_user_progress(db, user.id, 1, unlock=True)
    db.commit()
    return user


def ensure_user_progress(
    db: Session, user_id: int, level_id: int, unlock: bool = False
) -> UserLevelProgress:
    progress = (
        db.query(UserLevelProgress)
        .filter(
            UserLevelProgress.user_id == user_id,
            UserLevelProgress.level_id == level_id,
        )
        .first()
    )

    if progress is None:
        progress = UserLevelProgress(
            user_id=user_id,
            level_id=level_id,
            is_unlocked=unlock,
            is_completed=False,
            current_part=1,
            score=0,
            attempts=0,
            part_data=to_json({}),
            level_data=to_json({}),
        )
        db.add(progress)
        db.flush()
    elif unlock and not progress.is_unlocked:
        progress.is_unlocked = True

    return progress


def unlock_next_level(user_id: int, completed_level_id: int, db: Session):
    next_id = completed_level_id + 1
    if db.query(Level).filter(Level.id == next_id).first() is None:
        return None

    ensure_user_progress(db, user_id, next_id, unlock=True)
    return next_id


def get_level_lock_reason(db: Session, user_id: int, level_id: int) -> Optional[str]:
    if level_id <= 1:
        return None

    previous_level = db.query(Level).filter(Level.id == level_id - 1).first()
    if previous_level is None:
        return f"Previous level {level_id - 1} does not exist"

    previous_progress = (
        db.query(UserLevelProgress)
        .filter(
            UserLevelProgress.user_id == user_id,
            UserLevelProgress.level_id == level_id - 1,
        )
        .first()
    )
    if previous_progress is None or not previous_progress.is_completed:
        return f"Complete Level {level_id - 1} before unlocking Level {level_id}"

    return None


def get_user_game_status(db: Session, user_id: int):
    levels = db.query(Level).order_by(Level.id).all()
    results = []

    for level in levels:
        progress = (
            db.query(UserLevelProgress)
            .filter(
                UserLevelProgress.user_id == user_id,
                UserLevelProgress.level_id == level.id,
            )
            .first()
        )
        if progress is None:
            progress = ensure_user_progress(db, user_id, level.id, unlock=level.id == 1)

        results.append(
            {
                "level_id": level.id,
                "title": level.title,
                "is_unlocked": progress.is_unlocked,
                "is_completed": progress.is_completed,
                "current_part": progress.current_part,
                "score": progress.score,
                "attempts": progress.attempts,
                "part_data": parse_json(progress.part_data),
                "level_data": parse_json(progress.level_data),
            }
        )

    db.commit()
    return results


def get_level_full_details(db: Session, level_id: int, user_id: Optional[int] = None):
    level = db.query(Level).filter(Level.id == level_id).first()
    if level is None:
        return None

    result = {
        "id": level.id,
        "title": level.title,
        "description": level.description,
        "config": parse_json(level.config),
        "parts": [
            {
                "order": part.order,
                "title": part.title,
                "description": part.description,
                "config": parse_json(part.config),
            }
            for part in sorted(level.parts, key=lambda p: p.order)
        ],
        "user_progress": None,
    }

    if user_id:
        progress = ensure_user_progress(db, user_id, level_id, unlock=level_id == 1)
        result["user_progress"] = {
            "is_unlocked": progress.is_unlocked,
            "is_completed": progress.is_completed,
            "current_part": progress.current_part,
            "score": progress.score,
            "attempts": progress.attempts,
            "completed_at": progress.completed_at,
            "part_data": parse_json(progress.part_data),
            "level_data": parse_json(progress.level_data),
        }
        db.commit()

    return result


def complete_level(
    db: Session, user_id: int, level_id: int, score_earned: int = 100, custom_data=None
):
    user = db.query(User).filter(User.id == user_id).first()
    level = db.query(Level).filter(Level.id == level_id).first()
    if user is None or level is None:
        return None, None, None

    progress = ensure_user_progress(db, user_id, level_id, unlock=True)
    was_completed = progress.is_completed
    progress.is_completed = True
    progress.is_unlocked = True
    progress.current_part = len(level.parts) if level.parts else 1
    progress.completed_at = datetime.now().isoformat()

    if not was_completed:
        progress.score += score_earned
        user.total_score += score_earned

    progress.level_data = to_json(
        {
            "completed_via": "unity",
            "last_completed_at": progress.completed_at,
            "custom_data": custom_data or {},
        }
    )

    if level_id >= user.cur_level_id:
        user.cur_level_id = level_id + 1

    next_level_id = unlock_next_level(user_id, level_id, db)
    db.commit()
    db.refresh(user)
    return user, progress, next_level_id


def validate_answer(part_config: dict, answer) -> bool:
    expected = part_config.get("expected_answer")
    if expected is None:
        return True
    return answer == expected
