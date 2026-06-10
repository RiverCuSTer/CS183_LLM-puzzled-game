# Responsible team member: Jiayu Guo, Zhiyan Lin; Description: Configures the SQLAlchemy database engine, session factory, and database initialization helpers.
import os
from pathlib import Path

from sqlalchemy import create_engine
from sqlalchemy.orm import declarative_base, sessionmaker


BACKEND_DIR = Path(__file__).resolve().parent
DEFAULT_DATABASE_URL = f"sqlite:///{BACKEND_DIR / 'game_progress.db'}"
DATABASE_URL = os.getenv("DATABASE_URL", DEFAULT_DATABASE_URL)

connect_args = {"check_same_thread": False} if DATABASE_URL.startswith("sqlite") else {}
engine = create_engine(DATABASE_URL, connect_args=connect_args)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
Base = declarative_base()


def init_database():
    import model_class  # noqa: F401

    Base.metadata.create_all(bind=engine)


def get_database_session():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
