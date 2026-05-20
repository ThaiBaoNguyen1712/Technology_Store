from fastapi import APIRouter, HTTPException

from app.api.engine.SceneRecommendationFilter import SceneRecommendationFilter
from app.api.engine.content_based import recommend

router = APIRouter()


@router.get("/")
def root():
    return {"status": "ok"}


@router.get("/health")
def health():
    return {"status": "healthy"}


@router.get("/content_based_filter/{product_sys_id}")
async def get_recommendations(product_sys_id: str, top_n: int = 5):
    recommendations = recommend(product_sys_id=product_sys_id, top_n=top_n)
    return {"product_sys_id": product_sys_id, "recommendations": recommendations}


@router.get("/api/v1/recommendation/{user_id}/{scene}/{product_sys_id}")
async def get_scene_recommendations(user_id: int, scene: str, product_sys_id: str = None, top_n: int = 10):
    recommender = SceneRecommendationFilter()

    if scene == "detail" and product_sys_id:
        recommendations = recommender.get_recommendations_detail(
            user_id=user_id,
            product_sys_id=product_sys_id,
            top_n=top_n,
        )
    elif scene == "wishlist":
        recommendations = recommender.get_recommendations_wishlist(
            user_id=user_id,
            top_n=top_n,
        )
    elif scene == "cart":
        recommendations = recommender.get_recommendations_cart(
            user_id=user_id,
            top_n=top_n,
        )
    else:
        raise HTTPException(status_code=400, detail="Invalid scene or missing product_sys_id for detail scene.")

    return {
        "user_id": user_id,
        "scene": scene,
        "recommendations": recommendations,
    }


@router.get("/api/v1/recommendation/homepage/{user_id}")
async def get_homepage_recommendations(user_id: int, top_n: int = 50):
    recommender = SceneRecommendationFilter()
    recommendations = recommender.get_recommendations_homepage(
        user_id=user_id,
        top_n=top_n,
    )
    return {
        "user_id": user_id,
        "scene": "homepage",
        "recommendations": recommendations,
    }
