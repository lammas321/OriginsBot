from discord.ext import commands
import discord
import json as js


class smpCog(commands.Cog):
  def __init__(self, bot):
    self.bot = bot

  @commands.command(name="smp")
  async def smp(self, ctx):
    with open("./db/config.json",'r') as config:
      configD = js.load(config)
    config = configD["config"]

    server = config["server"]
    
    await ctx.send("IP: `{0}`, PORT: `{1}`".format(server["ip"], server["port"]))



def setup(bot):
  bot.add_cog(smpCog(bot))