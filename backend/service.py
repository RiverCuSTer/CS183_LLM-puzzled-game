"""业务逻辑层 —— 不与 FastAPI 耦合，纯函数可测试"""
import json
from typing import Optional

from sqlalchemy.orm import Session

from model_class import User, Level, UserLevelProgress, LevelPart

# ============================================================
#  用户初始化
# ============================================================


def create_and_init_user(db: Session, username: str):
    """创建用户 + 确保 Level 1 存在 + 解锁第一关"""
    new_user = User(name=username)
    db.add(new_user)
    db.commit()
    db.refresh(new_user)

    ensure_level_one_exists(db)
    unlock_first_level(new_user.id, db)
    db.commit()
    return new_user


# ============================================================
#  关卡数据初始化
# ============================================================


def ensure_level_one_exists(db: Session):
    """如果 Level 1 还不存在则创建（含 3 个 Part）"""
    if db.query(Level).filter(Level.id == 1).first():
        return

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
            description="将 AI 生成的 prompt 拆分为子词 Token。",
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
            description="将 Token 映射为数字 ID，排序为正确整数序列。",
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


# ============================================================
#  关卡进度
# ============================================================


def unlock_first_level(user_id: int, db: Session):
    """为新用户解锁第一关"""
    level = db.query(Level).filter(Level.id == 1).first()
    if not level:
        return

    existing = (
        db.query(UserLevelProgress)
        .filter(
            UserLevelProgress.user_id == user_id, UserLevelProgress.level_id == 1
        )
        .first()
    )
    if existing:
        return

    progress = UserLevelProgress(
        user_id=user_id,
        level_id=1,
        is_unlocked=True,
        is_completed=False,
        current_part=1,
        score=0,
        attempts=0,
        part_data=json.dumps({}, ensure_ascii=False),
    )
    db.add(progress)


def unlock_next_level(user_id: int, completed_level_id: int, db: Session):
    """完成关卡后解锁下一关"""
    next_id = completed_level_id + 1
    next_level = db.query(Level).filter(Level.id == next_id).first()
    if not next_level:
        return None

    existing = (
        db.query(UserLevelProgress)
        .filter(
            UserLevelProgress.user_id == user_id,
            UserLevelProgress.level_id == next_id,
        )
        .first()
    )
    if existing:
        existing.is_unlocked = True
    else:
        progress = UserLevelProgress(
            user_id=user_id,
            level_id=next_id,
            is_unlocked=True,
            is_completed=False,
            current_part=1,
            score=0,
            attempts=0,
            part_data=json.dumps({}, ensure_ascii=False),
        )
        db.add(progress)

    return next_id


# ============================================================
#  查询
# ============================================================

_PART_DATA_EMPTY_FALLBACK: dict = {}


def _parse_json(raw: Optional[str]) -> dict:
    if not raw:
        return {}
    try:
        return json.loads(raw)
    except (json.JSONDecodeError, TypeError):
        return {}


def get_user_game_status(db: Session, user_id: int):
    """获取某用户所有关卡的进度列表"""
    progresses = (
        db.query(UserLevelProgress)
        .filter(UserLevelProgress.user_id == user_id)
        .all()
    )

    levels_status = []
    for p in progresses:
        lvl = db.query(Level).filter(Level.id == p.level_id).first()
        levels_status.append(
            {
                "level_id": p.level_id,
                "title": lvl.title if lvl else f"Level {p.level_id}",
                "is_unlocked": p.is_unlocked,
                "is_completed": p.is_completed,
                "current_part": p.current_part,
                "score": p.score,
                "attempts": p.attempts,
                "part_data": _parse_json(p.part_data),
            }
        )

    return sorted(levels_status, key=lambda x: x["level_id"])


def get_level_full_details(db: Session, level_id: int, user_id: Optional[int] = None):
    """获取关卡详情 + 可选附加用户进度"""
    level = db.query(Level).filter(Level.id == level_id).first()
    if not level:
        return None

    parts = (
        db.query(LevelPart)
        .filter(LevelPart.level_id == level_id)
        .order_by(LevelPart.order)
        .all()
    )

    result = {
        "id": level.id,
        "title": level.title,
        "description": level.description,
        "config": _parse_json(level.config),
        "parts": [
            {
                "order": p.order,
                "title": p.title,
                "description": p.description,
                "config": _parse_json(p.config),
            }
            for p in parts
        ],
        "user_progress": None,
    }

    if user_id:
        progress = (
            db.query(UserLevelProgress)
            .filter(
                UserLevelProgress.user_id == user_id,
                UserLevelProgress.level_id == level_id,
            )
            .first()
        )

        if progress:
            result["user_progress"] = {
                "is_unlocked": progress.is_unlocked,
                "is_completed": progress.is_completed,
                "current_part": progress.current_part,
                "score": progress.score,
                "attempts": progress.attempts,
                "completed_at": progress.completed_at,
                "part_data": _parse_json(progress.part_data),
                "level_data": _parse_json(progress.level_data),
            }
        else:
            result["user_progress"] = {
                "is_unlocked": False,
                "is_completed": False,
                "current_part": 1,
                "score": 0,
                "attempts": 0,
                "completed_at": None,
                "part_data": {},
                "level_data": None,
            }

    return result


# ============================================================
#  Part 答案校验（每个 Part 各自逻辑）
# ============================================================


def validate_part1(part_config: dict, answer: dict) -> bool:
    """Part 1: Tokenization —— 校验拆分后的子词列表"""
    expected_tokens = part_config.get("expected_tokens", [])
    submitted = answer if isinstance(answer, list) else answer.get("tokens", [])
    if not expected_tokens:
        return True
    return submitted == expected_tokens


def validate_part2(part_config: dict, answer: dict) -> bool:
    """Part 2: Token ID Mapping —— 校验映射 + 序列"""
    expected_mapping = part_config.get("expected_mapping", {})
    expected_sequence = part_config.get("expected_sequence", [])

    mapping = answer.get("token_ids", {}) if isinstance(answer, dict) else {}
    sequence = answer.get("sequence", []) if isinstance(answer, dict) else answer

    if expected_mapping and mapping != expected_mapping:
        return False
    if expected_sequence and sequence != expected_sequence:
        return False
    return True


def validate_part3(part_config: dict, answer: dict) -> bool:
    """Part 3: Embedding —— 校验位置坐标"""
    expected_positions = part_config.get("expected_positions", {})
    submitted = answer.get("positions", {}) if isinstance(answer, dict) else answer
    if not expected_positions:
        return True
    return submitted == expected_positions


PART_VALIDATORS = {
    1: validate_part1,
    2: validate_part2,
    3: validate_part3,
}
