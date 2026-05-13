"""数据库引擎与会话管理"""
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker, declarative_base

DATABASE_URL = "mysql+pymysql://root:gzhnd010609@localhost:3306/cs183?charset=utf8mb4"

engine = create_engine(DATABASE_URL)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
Base = declarative_base()


def init_database():
    """导入所有模型后建表（避免循环依赖）"""
    # 确保 model_class 中的模型被 Base 扫描到
    import model_class  # noqa: F401

    Base.metadata.create_all(bind=engine)


def get_database_session():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
