# Responsible team member: Jiayu Guo, Zhiyan Lin; Description: Bootstraps the backend database seed data and starts the FastAPI server with Uvicorn.
import uvicorn

from database import init_database
from game_admin_v2 import app
from service import seed_levels
from database import SessionLocal


def bootstrap():
    init_database()
    db = SessionLocal()
    try:
        seed_levels(db)
    finally:
        db.close()


if __name__ == "__main__":
    bootstrap()
    uvicorn.run(app, host="0.0.0.0", port=8000)
