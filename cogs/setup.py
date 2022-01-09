from discord.ext import commands
import discord
from discord.utils import get
import discord.utils as utils
import json as js


class setupCog(commands.Cog):
  def __init__(self, bot):
    self.bot = bot
  #@commands.has_permissions(administrator=True)
  @commands.command(name="set-mute")
  async def set_mute(self, ctx, roleID):
    with open("./db/config.json") as config:
      config = js.load(config)
      if roleID.isdigit():
        try:
          role = get(ctx.guild.roles, id=roleID)
          config["roles"]["mute"] = int(roleID)

          with open("./db/config.json", "w") as newconfig:
            js.dump(config, newconfig)
          
          await ctx.reply("{0} set as mute role".format(role))
        except commands.RoleNotFound:
          pass
      

def setup(bot):
  bot.add_cog(setupCog(bot))