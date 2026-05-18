"""SQLAlchemy 数据模型 + Pydantic 校验 Schema"""
from typing import Optional, Any, List

from pydantic import BaseModel
from sqlalchemy import Column, Integer, String, Boolean, Text, ForeignKey
from sqlalchemy.orm import relationship

from database import Base

# ============================================================
#  SQLAlchemy ORM 模型
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
    config = Column(Text, nullable=True)  # 关卡配置 (JSON)

    progress_records = relationship(
        "UserLevelProgress", back_populates="level", cascade="all, delete-orphan"
    )
    parts = relationship(
        "LevelPart", back_populates="level", cascade="all, delete-orphan"
    )


class LevelPart(Base):
    """关卡的 Part 定义——每关可以有 N 个 Part"""

    __tablename__ = "level_parts"
    id = Column(Integer, primary_key=True, index=True)
    level_id = Column(
        Integer, ForeignKey("Level.id", ondelete="CASCADE"), nullable=False
    )
    order = Column(Integer, nullable=False)  # Part 序号 (1, 2, 3…)
    title = Column(String(50), nullable=False)
    description = Column(String(250))
    config = Column(Text, nullable=True)  # 每 Part 专属配置 (JSON)

    level = relationship("Level", back_populates="parts")


class UserLevelProgress(Base):
    """用户—关卡 进度表（每用户每关卡一条记录）"""

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
    current_part = Column(Integer, default=1)
    score = Column(Integer, default=0)
    attempts = Column(Integer, default=0)
    completed_at = Column(String(30), nullable=True)
    # Part 级别数据 (JSON)，key = part 序号
    part_data = Column(Text, nullable=True)
    # 关卡整体自定义数据 (JSON)
    level_data = Column(Text, nullable=True)

    user = relationship("User", back_populates="level_progresses")
    level = relationship("Level", back_populates="progress_records")


# ============================================================
#  Pydantic 校验 Schema
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
    player_id: int
    level_id: int
    part_order: int
    score_earned: int = 0
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
    player_id: int
    level_id: int
    custom_data: Optional[Any] = None


class LevelSubmitResponse(BaseModel):
    message: str
    current_total_score: int
    is_level_completed: bool
    next_level_unlocked: Optional[int] = None
