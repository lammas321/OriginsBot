from discord.ext import commands
import discord
import json as js
from discord.utils import get
from asyncio import sleep

#check if command is dissabled
def check_cmd(commandName):
  with open("./db/CogsConfig.json") as config:
    config = js.load(config)

    for command in config["commands"]:
      if command["name"] == commandName:
        return True
#checks if user is a mod
def check_mod(ctx):
  with open("./db/config.json", "r") as config:
    config = js.load(config)


  roles = config["roles"]["modRoles"]
  is_mod = False
  for role in roles:

    role = get(ctx.guild.roles, id=role)
    for role in ctx.author.roles:
      is_mod = True
  
  return is_mod



class moderationCog(commands.Cog):
  def __init__(self, bot):
    self.bot = bot
  


  @commands.command(name="soft-mute", pass_context=True)
  @commands.cooldown(rate=1, per=10, type=commands.BucketType.user)
  async def mute(self, ctx, user:discord.Member, ITime, *reason):
    if not check_cmd("mute"):
      if check_mod(ctx):
        with open("./db/config.json", "r") as config:
          config = js.load(config)
          roleid = config["roles"]["mute"]
          if isinstance(roleid, int) and roleid != 0:
            role = get(ctx.guild.roles, id=roleid)
            
            await user.add_roles(role)
            

            Char = ITime[:-1]
            Char = Char.lower()
            if Char == "s":
              mult = 1
            elif Char == "m":
              mult = 60
            elif Char == "h":
              mult = 3600,
            elif Char == "w":
              mult = 604800
            else:
              mult = "undefined"
            
            if mult != "undefined":
              Time = ITime.replace("s", "")
              Time = ITime.replace("m", "")
              Time = ITime.replace("h", "")
              Time = ITime.replace("d", "")
              Time = ITime.replace("w", "")

              emb = discord.Embed(title="user has been muted", description="the user {0} has been muted by the moderator {1} for {2}".format(user, ctx.author, reason, color=discord.Color.dark_orange()))
              Time *= mult

              await sleep(Time)

              await user.remove_roles(role)
            else:
              raise commands.UserInputError

      else:
        emb = discord.Embed(title="missing permissions", description="you are missing a required role to use this command", color=discord.Color.red())
    
    else:
      emb = discord.Embed(title="command disabled", description="this command has ben disabled by a dev")
    
    await ctx.send(embed=emb)


  @commands.command(name="unmute")
  async def unmute(self, ctx, user:discord.Member):
    with open("./db/config.json", "r") as config:
      config = js.load(config)

      roleid = config["roles"]["mute"]

def setup(bot):
  bot.add_cog(moderationCog(bot))