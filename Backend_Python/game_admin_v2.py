from fastapi import FastAPI, HTTPException, Depends, status
from sqlalchemy import create_engine, Column, Integer, String, Boolean, Text, ForeignKey
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker, Session, relationship
from pydantic import BaseModel
from typing import List, Optional, Any
from datetime import datetime
import json
import random

# --- 数据库配置 ---
SQLALCHEMY_DATABASE_URL = "sqlite:///./game_admin_v2.db"
engine = create_engine(
    SQLALCHEMY_DATABASE_URL, connect_args={"check_same_thread": False}
)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
Base = declarative_base()


# ============================================================
#  数据库模型 (Models)
# ============================================================


class User(Base):
    __tablename__ = "users"
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(50), nullable=False, unique=True)
    cur_level_id = Column(Integer, default=1)
    total_score = Column(Integer, default=0)

    level_progresses = relationship(
        "UserLevelProgress", back_populates="user", cascade="all, delete-orphan"
    )


class Level(Base):
    __tablename__ = "Level"
    id = Column(Integer, primary_key=True, index=True)
    title = Column(String(50), nullable=False)
    description = Column(String(250))
    # 关卡验证，配置数据（JSON）
    config = Column(Text, nullable=True)

    progress_records = relationship(
        "UserLevelProgress", back_populates="level", cascade="all, delete-orphan"
    )
    parts = relationship(
        "LevelPart", back_populates="level", cascade="all, delete-orphan"
    )


class LevelPart(Base):
    """
    关卡的 Part 定义
    每关可以有 N 个 Part，按最终的策划案排序
    """

    __tablename__ = "level_parts"
    id = Column(Integer, primary_key=True, index=True)
    level_id = Column(
        Integer, ForeignKey("Level.id", ondelete="CASCADE"), nullable=False
    )
    order = Column(Integer, nullable=False)  # Part 序号 (1, 2, 3...)
    title = Column(String(50), nullable=False)
    description = Column(String(250))
    config = Column(Text, nullable=True)  # 每 Part 专属配置（JSON）

    level = relationship("Level", back_populates="parts")


class UserLevelProgress(Base):
    """
    用户-关卡 进度表
    每用户 每关卡 一条记录
    """

    __tablename__ = "user_level_progress"
    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(
        Integer, ForeignKey("users.id", ondelete="CASCADE"), nullable=False
    )
    level_id = Column(
        Integer, ForeignKey("Level.id", ondelete="CASCADE"), nullable=False
    )
    is_unlocked = Column(Boolean, default=False)
    is_completed = Column(Boolean, default=False)
    current_part = Column(Integer, default=1)  # 当前进行到的 Part 序号
    score = Column(Integer, default=0)
    attempts = Column(Integer, default=0)
    completed_at = Column(String(30), nullable=True)

    # Part 级别数据（JSON 对象，key = part序号）
    # 例: {"1": {"prompt": "...", "tokens": [...]}, "2": {...}, "3": {...}}
    part_data = Column(Text, nullable=True)

    # 关卡整体自定义数据（可选，汇总用，反正最后要给用户提供学习记录不是）
    level_data = Column(Text, nullable=True)

    user = relationship("User", back_populates="level_progresses")
    level = relationship("Level", back_populates="progress_records")


# ============================================================
#  Pydantic验证模型 (Schemas)
# ============================================================


class UserBase(BaseModel):
    name: str
    cur_level_id: int = 1
    total_score: int = 0


class UserCreate(UserBase):
    pass


class UserOut(UserBase):
    id: int

    class Config:
        from_attributes = True


class PartOut(BaseModel):
    order: int
    title: str
    description: Optional[str] = None
    config: Optional[Any] = None

    class Config:
        from_attributes = True


class LevelDetailOut(BaseModel):
    id: int
    title: str
    description: Optional[str] = None
    parts: List[PartOut]
    config: Optional[Any] = None
    user_progress: Optional[dict] = None

    class Config:
        from_attributes = True


class PartSubmitRequest(BaseModel):
    """Part的提交"""

    player_id: int
    level_id: int
    part_order: int  # 提交第 n个 Part (1, 2, 3)
    score_earned: int = 0
    # 各 Part 可以带自定义数据
    # Part 1: tokens=["子词1", "子词2", ...]
    # Part 2: token_ids={"token1": id1, "token2": id2, ...}, sequence=[id1, id2, ...]
    # Part 3: embedding_positions={"token": [x, y], ...}
    answer: Optional[Any] = None


class PartSubmitResponse(BaseModel):
    message: str
    is_correct: bool
    is_part_completed: bool
    is_level_completed: bool
    current_part: int
    current_total_score: int
    next_level_unlocked: Optional[int] = None


class LevelSubmitRequest(BaseModel):
    """仅当所有 Part 完成后，才可以标记该关卡完成"""

    player_id: int
    level_id: int
    custom_data: Optional[Any] = None


class LevelSubmitResponse(BaseModel):
    message: str
    current_total_score: int
    is_level_completed: bool
    next_level_unlocked: Optional[int] = None


# 创建数据库表
Base.metadata.create_all(bind=engine)

# ============================================================
#  数据库依赖
# ============================================================


def get_database_session():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


app = FastAPI(
    title="LLM Puzzle Game Backend",
    version="2.0.0",
    description="Educational Puzzle Game — Level 1: Text Digitization (3 Parts)",
)


# ============================================================
#  核心函数
# ============================================================


def unlock_first_level(user_id: int, db: Session):
    """为新用户解锁第一关"""
    level = db.query(Level).filter(Level.id == 1).first()
    if not level:
        return

    existing = (
        db.query(UserLevelProgress)
        .filter(UserLevelProgress.user_id == user_id, UserLevelProgress.level_id == 1)
        .first()
    )
    if not existing:
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
    """完成关卡后，解锁下一关"""
    next_level = db.query(Level).filter(Level.id == completed_level_id + 1).first()
    if not next_level:
        return None

    existing = (
        db.query(UserLevelProgress)
        .filter(
            UserLevelProgress.user_id == user_id,
            UserLevelProgress.level_id == next_level.id,
        )
        .first()
    )
    if not existing:
        progress = UserLevelProgress(
            user_id=user_id,
            level_id=next_level.id,
            is_unlocked=True,
            is_completed=False,
            current_part=1,
            score=0,
            attempts=0,
            part_data=json.dumps({}, ensure_ascii=False),
        )
        db.add(progress)
    else:
        existing.is_unlocked = True

    return next_level.id


# ============================================================
#  Part 答案校验（每 Part 各自逻辑）
# ============================================================


def validate_part1(part_config: dict, answer: dict) -> bool:
    """
    Part 1: Tokenization
    玩家提交 tokens 列表
    校验：拆分后的子词是否与预期一致
    """
    expected_tokens = part_config.get("expected_tokens", [])
    submitted = answer if isinstance(answer, list) else answer.get("tokens", [])

    if not expected_tokens:
        return True  # 如果没有预期答案，默认通过
    return submitted == expected_tokens


def validate_part2(part_config: dict, answer: dict) -> bool:
    """
    Part 2: Token ID Mapping
    玩家提交 token→ID 映射 和 整数序列
    校验：映射正确 + 排序正确
    """
    expected_mapping = part_config.get("expected_mapping", {})
    expected_sequence = part_config.get("expected_sequence", [])

    mapping = answer.get("token_ids", {}) if isinstance(answer, dict) else {}
    sequence = answer.get("sequence", []) if isinstance(answer, dict) else answer

    # 校验映射
    if expected_mapping and mapping != expected_mapping:
        return False
    # 校验序列
    if expected_sequence and sequence != expected_sequence:
        return False

    return True


def validate_part3(part_config: dict, answer: dict) -> bool:
    """
    Part 3: Embedding（向量表示）
    玩家提交每个 token 的 [x, y] 坐标
    前端已有 3D 坐标系交互，后端仅记录结果(我还在等
    如果配置了预期坐标则校验
    """
    expected_positions = part_config.get("expected_positions", {})
    submitted = answer.get("positions", {}) if isinstance(answer, dict) else answer

    if not expected_positions:
        return True  # Part 3 可能是自由探索，默认通过
    return submitted == expected_positions


PART_VALIDATORS = {
    1: validate_part1,
    2: validate_part2,
    3: validate_part3,
}


# ============================================================
#  API 端点
# ============================================================


@app.get("/", tags=["General"])
def check_server_status():
    return {
        "status": "online",
        "message": "LLM Puzzle Game — Level 1: Text Digitization",
    }


# ---------- 1. 登录 ----------


@app.post("/login", tags=["Game Flow"])
def player_login(username: str, db: Session = Depends(get_database_session)):
    user = db.query(User).filter(User.name == username).first()

    if user:
        progresses = (
            db.query(UserLevelProgress)
            .filter(UserLevelProgress.user_id == user.id)
            .all()
        )

        levels_status = []
        for p in progresses:
            lvl = db.query(Level).filter(Level.id == p.level_id).first()
            part_data_obj = json.loads(p.part_data) if p.part_data else {}
            levels_status.append(
                {
                    "level_id": p.level_id,
                    "title": lvl.title if lvl else f"Level {p.level_id}",
                    "is_unlocked": p.is_unlocked,
                    "is_completed": p.is_completed,
                    "current_part": p.current_part,
                    "score": p.score,
                    "attempts": p.attempts,
                    "part_data": part_data_obj,
                }
            )

        return {
            "status": "success",
            "message": "读取存档成功",
            "user_data": user,
            "level_progress": sorted(levels_status, key=lambda x: x["level_id"]),
        }
    else:
        new_user = User(name=username)
        db.add(new_user)
        db.commit()
        db.refresh(new_user)

        # 确保 Level 1 存在
        level_one = db.query(Level).filter(Level.id == 1).first()
        if not level_one:
            level_one = Level(
                id=1,
                title="Text Digitization (文本数字化)",
                description="通过 Tokenization → Token ID Mapping → Embedding 三步理解 LLM 如何处理文本。",
                config=json.dumps({"total_parts": 3}, ensure_ascii=False),
            )
            db.add(level_one)
            db.flush()

            # 添加 3 个 Parts
            parts_data = [
                {
                    "order": 1,
                    "title": "Tokenization",
                    "description": "将 AI 生成的 prompt 拆分为子词 Token。",
                    "config": json.dumps(
                        {
                            "prompt_templates": [
                                "The quick brown fox jumps over the lazy dog near the riverbank.",
                                "Artificial intelligence models learn from vast amounts of text data every day.",
                                "Transformer architecture revolutionized natural language processing tasks.",
                            ],
                            "expected_tokens": None,  # 前端动态确定
                        },
                        ensure_ascii=False,
                    ),
                },
                {
                    "order": 2,
                    "title": "Token ID Mapping",
                    "description": "将 Token 映射为 ID，排序为整数序列。重复 3 次。",
                    "config": json.dumps(
                        {
                            "rounds": 3,
                        },
                        ensure_ascii=False,
                    ),
                },
                {
                    "order": 3,
                    "title": "Vector Representation (Embedding)",
                    "description": "将 Token 序列放入 3D 向量空间，感受词义相近的向量靠近。",
                    "config": json.dumps({}, ensure_ascii=False),
                },
            ]
            for p in parts_data:
                part = LevelPart(level_id=1, **p)
                db.add(part)
            db.commit()

        # 解锁第一关
        unlock_first_level(new_user.id, db)
        db.commit()

        level_one = db.query(Level).filter(Level.id == 1).first()
        return {
            "status": "success",
            "message": "新账号创建成功，Level 1 Part 1 已解锁",
            "user_data": new_user,
            "level_progress": [
                {
                    "level_id": 1,
                    "title": level_one.title,
                    "is_unlocked": True,
                    "is_completed": False,
                    "current_part": 1,
                    "score": 0,
                    "attempts": 0,
                    "part_data": {},
                }
            ],
        }


# ---------- 2. 关卡详情（含 Parts 列表）----------


@app.get("/level/{level_id}", tags=["Game Flow"])
def fetch_level_detail(
    level_id: int,
    user_id: Optional[int] = None,
    db: Session = Depends(get_database_session),
):
    level = db.query(Level).filter(Level.id == level_id).first()
    if not level:
        raise HTTPException(status_code=404, detail=f"Level {level_id} 不存在")

    # Part 列表
    parts = (
        db.query(LevelPart)
        .filter(LevelPart.level_id == level_id)
        .order_by(LevelPart.order)
        .all()
    )
    parts_out = []
    for p in parts:
        cfg = json.loads(p.config) if p.config else {}
        # 对 Part 2 不暴露 expected_tokens（玩家需要自己去匹配）
        parts_out.append(
            {
                "order": p.order,
                "title": p.title,
                "description": p.description,
                "config": cfg,
            }
        )

    result = {
        "id": level.id,
        "title": level.title,
        "description": level.description,
        "parts": parts_out,
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
            part_data_obj = json.loads(progress.part_data) if progress.part_data else {}
            level_data_obj = (
                json.loads(progress.level_data) if progress.level_data else None
            )

            result["user_progress"] = {
                "is_unlocked": progress.is_unlocked,
                "is_completed": progress.is_completed,
                "current_part": progress.current_part,
                "score": progress.score,
                "attempts": progress.attempts,
                "completed_at": progress.completed_at,
                "part_data": part_data_obj,
                "level_data": level_data_obj,
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


# ---------- 3. 获取某个 Part 的初始数据 ----------


@app.get("/level/{level_id}/part/{part_order}", tags=["Game Flow"])
def fetch_part_data(
    level_id: int,
    part_order: int,
    user_id: int,
    db: Session = Depends(get_database_session),
):
    """返回前端在 Part 中需要展示的初始数据"""
    level = db.query(Level).filter(Level.id == level_id).first()
    if not level:
        raise HTTPException(status_code=404, detail="Level 不存在")

    part = (
        db.query(LevelPart)
        .filter(
            LevelPart.level_id == level_id,
            LevelPart.order == part_order,
        )
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

    part_config = json.loads(part.config) if part.config else {}
    part_data_obj = json.loads(progress.part_data) if progress.part_data else {}

    # 如果这个 Part 已完成，返回之前的数据
    stored = part_data_obj.get(str(part_order), {})

    if part_order == 1:
        # Part 1: 返回一个随机 prompt 让玩家拆分
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

    elif part_order == 2:
        # Part 2: 基于 Part 1 的 token，生成 Token→ID 映射任务
        p1_data = part_data_obj.get("1", {})
        tokens = p1_data.get("tokens", [])
        if not tokens:
            # 如果没有 Part 1 的 token，用默认 demo token
            tokens = ["The", "quick", "brown", "fox", "jumps"]

        # 模拟词表：从 tokens 生成一些 ID
        vocab = {}
        for i, tok in enumerate(tokens):
            vocab[tok] = 100 + i * 10

        # 混入一些干扰 token-ID 对
        distractor_vocab = {
            "slow": 50,
            "cat": 60,
            "river": 70,
            "learn": 80,
            "data": 90,
        }
        full_vocab = {**vocab, **distractor_vocab}

        return {
            "part_order": 2,
            "title": part.title,
            "description": part.description,
            "rounds": part_config.get("rounds", 3),
            "current_round": stored.get("current_round", 1),
            "tokens_to_map": tokens,
            "vocabulary": full_vocab,
            "hint": "将每个 Token 与词表中的 ID 一一对应，然后排序为正确的整数序列。",
            "stored_data": stored,
        }

    elif part_order == 3:
        # Part 3: 基于 Part 1&2 的 token 序列
        p1_data = part_data_obj.get("1", {})
        tokens = p1_data.get("tokens", [])
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


# ---------- 4. Part 123 的提交 ----------


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
    """提交某个 Part 的完成结果"""
    # 校验
    db_user = db.query(User).filter(User.id == data.player_id).first()
    if not db_user:
        raise HTTPException(status_code=404, detail="未找到该玩家")

    level = db.query(Level).filter(Level.id == level_id).first()
    if not level:
        raise HTTPException(status_code=404, detail="关卡不存在")

    part = (
        db.query(LevelPart)
        .filter(
            LevelPart.level_id == level_id,
            LevelPart.order == part_order,
        )
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

    # 检查 Part 顺序：只能提交 current_part 或者已完成的 Part
    if part_order > progress.current_part and not progress.is_completed:
        raise HTTPException(
            status_code=400, detail=f"请先完成 Part {progress.current_part}"
        )

    # 校验答案
    part_config = json.loads(part.config) if part.config else {}
    validator = PART_VALIDATORS.get(part_order)
    is_correct = validator(part_config, data.answer) if validator else True

    # 更新 part_data
    part_data_obj = json.loads(progress.part_data) if progress.part_data else {}
    submission_entry = {
        "submitted_at": datetime.now().isoformat(),
        "answer": data.answer,
        "is_correct": is_correct,
        "score_earned": data.score_earned if is_correct else 0,
    }

    existing_part_data = part_data_obj.get(str(part_order), {})
    if isinstance(existing_part_data, dict):
        # 如果是多次尝试，保留历史
        attempts = existing_part_data.get("attempts", [])
        attempts.append(submission_entry)
        existing_part_data["attempts"] = attempts
        existing_part_data["last_submission"] = submission_entry
        if is_correct:
            existing_part_data["completed"] = True
            existing_part_data["final_answer"] = data.answer
        part_data_obj[str(part_order)] = existing_part_data
    else:
        part_data_obj[str(part_order)] = {
            "attempts": [submission_entry],
            "last_submission": submission_entry,
        }

    progress.part_data = json.dumps(part_data_obj, ensure_ascii=False)
    progress.attempts += 1

    is_part_completed = is_correct
    is_level_completed = False
    next_level_id = None

    if is_correct:
        progress.score += data.score_earned
        db_user.total_score += data.score_earned

        # 检查是否所有 Part 都完成了
        total_parts = db.query(LevelPart).filter(LevelPart.level_id == level_id).count()
        completed_parts = 0
        for i in range(1, total_parts + 1):
            pd = part_data_obj.get(str(i), {})
            if isinstance(pd, dict) and pd.get("last_submission", {}).get("is_correct"):
                completed_parts += 1

        if completed_parts >= total_parts:
            # 所有 Part 完成 → 关卡完成
            progress.is_completed = True
            progress.completed_at = datetime.now().isoformat()
            if level_id >= db_user.cur_level_id:
                db_user.cur_level_id = level_id + 1
            is_level_completed = True
            next_level_id = unlock_next_level(data.player_id, level_id, db)
        else:
            # 前进到下一个 Part
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


# ---------- 5. 用户进度总览 ----------


@app.get("/users/{user_id}/progress", tags=["Game Flow"])
def fetch_user_progress(user_id: int, db: Session = Depends(get_database_session)):
    user = db.query(User).filter(User.id == user_id).first()
    if not user:
        raise HTTPException(status_code=404, detail="未找到该玩家")

    progresses = (
        db.query(UserLevelProgress).filter(UserLevelProgress.user_id == user_id).all()
    )

    results = []
    for p in progresses:
        lvl = db.query(Level).filter(Level.id == p.level_id).first()
        part_data_obj = json.loads(p.part_data) if p.part_data else {}
        level_data_obj = json.loads(p.level_data) if p.level_data else None

        # 计算 Parts 完成状态
        parts_status = []
        total_parts = (
            db.query(LevelPart).filter(LevelPart.level_id == p.level_id).count()
        )
        for i in range(1, total_parts + 1):
            pd = part_data_obj.get(str(i), {})
            completed = False
            if isinstance(pd, dict):
                completed = pd.get("last_submission", {}).get(
                    "is_correct", False
                ) or pd.get("completed", False)
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
                "level_data": level_data_obj,
            }
        )

    return {
        "user_id": user.id,
        "username": user.name,
        "total_score": user.total_score,
        "current_level_id": user.cur_level_id,
        "progress": sorted(results, key=lambda x: x["level_id"]),
    }


# ---------- 6. 关卡管理 ----------


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


# ---------- 7. 用户管理 ----------


@app.get("/users/", response_model=List[UserOut], tags=["User Management"])
def fetch_all_users(
    skip: int = 0, limit: int = 100, db: Session = Depends(get_database_session)
):
    return db.query(User).offset(skip).limit(limit).all()


@app.get("/users/{user_id}", response_model=UserOut, tags=["User Management"])
def fetch_user_by_id(user_id: int, db: Session = Depends(get_database_session)):
    user = db.query(User).filter(User.id == user_id).first()
    if not user:
        raise HTTPException(status_code=404, detail=f"User with ID {user_id} not found")
    return user


@app.post(
    "/users/",
    response_model=UserOut,
    status_code=status.HTTP_201_CREATED,
    tags=["User Management"],
)
def register_new_user(user_in: UserCreate, db: Session = Depends(get_database_session)):
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
    update_data = user_update.model_dump()
    for key, value in update_data.items():
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


# ---------- 8. 初始化关卡数据 ----------


@app.post("/seed/level1", tags=["Development"])
def seed_level1(db: Session = Depends(get_database_session)):
    """填充 Level 1 数据"""
    existing = db.query(Level).filter(Level.id == 1).first()
    if existing:
        return {"message": "Level 1 已存在，跳过填充"}

    level = Level(
        id=1,
        title="Text Digitization (文本数字化)",
        description="通过 Tokenization → Token ID Mapping → Embedding 三步理解 LLM 如何处理文本。",
        config=json.dumps({"total_parts": 3}, ensure_ascii=False),
    )
    db.add(level)
    db.flush()

    parts_data = [
        {
            "order": 1,
            "title": "Tokenization",
            "description": "将 AI 生成的 prompt 拆分为子词 Token。删去无关信息，用分词标识填入空隙。",
            "config": json.dumps(
                {
                    "prompt_templates": [
                        # 这里我让ai随机生产了几段prompt，等api接进来再慢慢改具体数据
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
        },
        {
            "order": 2,
            "title": "Token ID Mapping",
            "description": "将 Token 映射为数字 ID，排序为正确整数序列。重复 3 轮。",
            "config": json.dumps({"rounds": 3}, ensure_ascii=False),
        },
        {
            "order": 3,
            "title": "Vector Representation (Embedding)",
            "description": "将 Token 序列放入 3D 向量空间，词义相近的 Token 互相靠近闪烁。",
            "config": json.dumps({}, ensure_ascii=False),
        },
    ]

    for p in parts_data:
        part = LevelPart(level_id=1, **p)
        db.add(part)

    db.commit()
    return {"message": "Level 1 填充完成（3 个 Parts）"}
