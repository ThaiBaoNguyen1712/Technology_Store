from urllib.parse import quote_plus

from sqlalchemy import create_engine

import env

engine = create_engine(
    f"mssql+pyodbc:///?odbc_connect={quote_plus(env.connection_string)}",
    pool_pre_ping=True,
)
