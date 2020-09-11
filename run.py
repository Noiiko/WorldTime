#!/usr/bin/env python3

import asyncpg

from bot import WorldTime
import config
from source.utils import common
from source.utils.userdb import DatabaseUtil

common.log_print("World Time", f"World Time v {config.bot_version}")

bot = WorldTime()
bot.run()
