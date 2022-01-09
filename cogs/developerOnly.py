from discord.ext import commands
import discord
import json as js

class devCommandsCog(commands.Cog):
  def __init__(self, bot):
    self.bot = bot
  
  @commands.command(name="load_unloaded")
  async def load_unloaded(self, ctx):
    with open("./db/config.json") as config:
      config = js.load(config)



def setup(bot):
  bot.add_cog(devCommandsCog(bot))