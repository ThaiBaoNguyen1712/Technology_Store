from fastapi import FastAPI
from fastapi.concurrency import asynccontextmanager
from app.api.engine.content_based import load_all_data
from app.api.routes import router

@asynccontextmanager
async def lifespan(app: FastAPI):
    # This runs when the app starts
    print("Initializing ML data...")
    try:
        load_all_data()
        print("ML data initialization complete.")
    except Exception as e:
        print(f"Error initializing ML data: {e}")
    yield
    # This runs when the app stops
    print("Shutting down application...")

app = FastAPI(
    title="Products Recommendation System API",
    description="API for products recommendation system",
    version="1.0.0",
    lifespan=lifespan
)

# Add routes from routes.py
app.include_router(router)

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)
