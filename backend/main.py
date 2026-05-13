"""应用入口 —— 仅做启动"""
import uvicorn
from database import init_database
from game_admin_v2 import app

# 启动时自动建表
init_database()

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)
