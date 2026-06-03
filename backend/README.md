# LLM Puzzle Game Backend

FastAPI progress backend for the Unity project.

## Setup

```bash
cd backend
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python main.py
```

The server listens on:

```text
http://127.0.0.1:8000
```

Swagger API docs:

```text
http://127.0.0.1:8000/docs
```

## Database

By default the backend uses local SQLite:

```text
backend/game_progress.db
```

To use MySQL instead, set `DATABASE_URL` before starting:

```bash
set DATABASE_URL=mysql+pymysql://root:password@localhost:3306/cs183?charset=utf8mb4
python main.py
```

## Unity Flow

Unity logs in with:

```text
POST /login?username=Player
```

When a level is completed, Unity calls:

```text
POST /level/{level_id}/complete
```

Request body:

```json
{
  "player_id": 1,
  "level_id": 4,
  "score_earned": 100
}
```

The backend seeds Levels 1-4 automatically on startup.
