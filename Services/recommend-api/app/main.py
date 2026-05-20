from fastapi import FastAPI
from fastapi.concurrency import asynccontextmanager

from app.api.engine.content_based import load_all_data
from app.api.routes import router


@asynccontextmanager
async def lifespan(app: FastAPI):
    print("Initializing ML data...")
    load_all_data()
    yield
    print("Shutting down application...")


app = FastAPI(
    title="Products Recommendation System API",
    description="API for products recommendation system",
    version="1.0.0",
    lifespan=lifespan,
)

app.include_router(router)


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)
