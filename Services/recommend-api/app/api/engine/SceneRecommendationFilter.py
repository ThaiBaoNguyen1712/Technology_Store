import pandas as pd
import redis

from app.api.engine.content_based import recommend
from db.mssql import engine
from env import redis_host, redis_password, redis_port, redis_ssl


class SceneRecommendationFilter:
    def __init__(self):
        self.engine = engine
        self.redis_client = redis.Redis(
            host=redis_host,
            port=redis_port,
            password=redis_password or None,
            ssl=redis_ssl,
            decode_responses=True,
        )

    def get_exclude_idx(self, user_id: int):
        purchased_query = "SELECT DISTINCT p.product_sys_id FROM OrderItem oi JOIN [Order] o ON oi.order_id = o.order_id JOIN Product p ON oi.product_id = p.product_id WHERE o.user_id = ?"
        cart_query = "SELECT DISTINCT p.product_sys_id FROM CartItem ci JOIN Cart c ON ci.cart_id = c.cart_id JOIN Product p ON ci.product_id = p.product_id WHERE c.user_id = ?"

        purchased_df = pd.read_sql(purchased_query, self.engine, params=(user_id,))
        cart_df = pd.read_sql(cart_query, self.engine, params=(user_id,))

        return (
            set(purchased_df["product_sys_id"].astype(str).str.strip()),
            set(cart_df["product_sys_id"].astype(str).str.strip()),
        )

    def get_recommendations_homepage(self, user_id: int, top_n: int = 50):
        list_key = f"user:{user_id}:latest_watched" if user_id else f"guest:{user_id}:latest_watched"
        recent_viewed = [str(pid).strip() for pid in self.redis_client.lrange(list_key, 0, 9)]

        if not recent_viewed:
            return []

        purchased_set, cart_set = self.get_exclude_idx(user_id)
        exclude = purchased_set.union(cart_set)

        candidate_scores = {}
        for pid in recent_viewed:
            similar_ids = recommend(pid, top_n=10)
            for sim_id in similar_ids:
                candidate_scores[sim_id] = candidate_scores.get(sim_id, 0) + 1

        final_list = sorted(candidate_scores.items(), key=lambda x: x[1], reverse=True)
        return [pid for pid, _score in final_list if pid not in exclude][:top_n]

    def get_recommendations_detail(self, user_id: int, product_sys_id: str, top_n: int = 11):
        recommendations = recommend(product_sys_id, top_n=top_n + 5)
        purchased_set, cart_set = self.get_exclude_idx(user_id)

        exclude = purchased_set.union(cart_set)
        exclude.add(str(product_sys_id))

        return [pid for pid in recommendations if pid not in exclude][:top_n]

    def get_recommendations_wishlist(self, user_id: int, top_n: int = 50):
        wl_query = "SELECT DISTINCT p.product_sys_id FROM Wishlist wl JOIN Product p ON wl.product_id = p.product_id WHERE wl.user_id = ?"
        wl_df = pd.read_sql(wl_query, self.engine, params=(user_id,))
        wishlist_ids = wl_df["product_sys_id"].astype(str).str.strip().tolist()

        if not wishlist_ids:
            return []

        purchased_set, cart_set = self.get_exclude_idx(user_id)
        exclude = purchased_set.union(cart_set).union(set(wishlist_ids))

        candidate_scores = {}
        for pid in wishlist_ids:
            for sim_id in recommend(pid, top_n=10):
                candidate_scores[sim_id] = candidate_scores.get(sim_id, 0) + 1

        final_list = sorted(candidate_scores.items(), key=lambda x: x[1], reverse=True)
        return [pid for pid, _score in final_list if pid not in exclude][:top_n]

    def get_recommendations_cart(self, user_id: int, top_n: int = 15):
        purchased_set, cart_set = self.get_exclude_idx(user_id)

        if not cart_set:
            return []

        candidate_scores = {}
        for pid in cart_set:
            for sim_id in recommend(pid, top_n=10):
                candidate_scores[sim_id] = candidate_scores.get(sim_id, 0) + 1

        exclude = purchased_set.union(cart_set)
        final_list = sorted(candidate_scores.items(), key=lambda x: x[1], reverse=True)
        return [pid for pid, _score in final_list if pid not in exclude][:top_n]
