from discord.ext import commands
import discord
import json as js
from discord.utils import get
from asyncio import sleep
from re import sub

#check if command is disabled
def check_cmd(commandName):
  with open("./db/CogsConfig.json") as config:
    config = js.load(config)
    for command in config["commands"]:
      if command["name"] == commandName:
        return command["disabled"]

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
  
  @commands.command(name="mute", pass_context=True)
  @commands.cooldown(rate=1, per=10, type=commands.BucketType.user)
  async def mute(self, ctx, user:discord.Member, Time, *reason):
    dont_send = False
    if not check_cmd("mute"):
      if check_mod(ctx):
        with open("./db/config.json", "r") as config:
          config = js.load(config)
          roleid = config["roles"]["mute"]
          if isinstance(roleid, int) and roleid != 0:
            role = get(ctx.guild.roles, id=roleid)
            
            await user.add_roles(role)
          
            Char = Time[-1]
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

              emb = discord.Embed(title="user has been muted", description="the user {0} has been muted by the moderator {1} for {2}".format(user, ctx.author, " ".join(reason), color=discord.Color.dark_orange()))
              Time = sub("[^0-9]", "", Time)
              Time = int(Time)
              Time *= mult
              await ctx.send(embed=emb)
              dont_send = True
              await sleep(Time)

              await user.remove_roles(role)
            else:
              raise commands.UserInputError
      else:
        emb = discord.Embed(title="missing permissions", description="you are missing a required role to use this command", color=discord.Color.red())
    else:
      emb = discord.Embed(title="command disabled", description="this command has been disabled by a dev")
    if dont_send == False:
      await ctx.send(embed=emb)


  @commands.command(name="unmute")
  async def unmute(self, ctx, user:discord.Member):
    if not check_cmd("unmute"):
      if check_mod(ctx):
        with open("./db/config.json", "r") as config:
          config = js.load(config)

          roleid = config["roles"]["mute"]
          removed = False
          for role in user.roles:
            if role.id == roleid:
              await user.remove_roles(role)
              removed = True
              emb = discord.Embed(title="user unmuted")
            
          if removed == False:
            emb = discord.Embed(title="user not muted")
          
          await ctx.send(embed = emb)

  

  @commands.command(name="ban")
  @commands.cooldown(rate=1, per=10, type=commands.BucketType.user)
  async def ban(self, ctx, user:discord.Member, *reason):
    if not check_cmd("ban"):
      if check_mod(ctx):
        await user.ban(reason=" ".join(reason))

  @commands.command(name="unban")
  async def unban(self, ctx, id):
    if not check_cmd("unban"):
      if check_mod(ctx):
        user = await self.bot.fetch_user(id)
        await ctx.guild.unban(user)

  @commands.command(name="kick")
  async def kick(self, ctx, user:discord.Member):
    if not check_cmd("kick"):
      if check_mod(ctx):
        await user.kick()

    emb = discord.Embed(title="member was kicked")
    await ctx.send(embed=emb)

    @commands.command(name="vcmute")
    async def vcmute(self, ctx, user:discord.Member, Time, *reason):
      dont_send = False
      if not check_cmd("mute"):
        if check_mod(ctx):
          with open("./db/config.json", "r") as config:
            config = js.load(config)
            roleid = config["roles"]["mute"]
            if isinstance(roleid, int) and roleid != 0:
              role = get(ctx.guild.roles, id=roleid)
              
              await user.add_roles(role)
            
              Char = Time[-1]
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

                emb = discord.Embed(title="user has been muted", description="the user {0} has been muted by the moderator {1} for {2}".format(user, ctx.author, " ".join(reason), color=discord.Color.dark_orange()))
                Time = sub("[^0-9]", "", Time)
                Time = int(Time)
                Time *= mult
                await ctx.send(embed=emb)
                dont_send = True
                await sleep(Time)

                await user.remove_roles(role)
              else:
                raise commands.UserInputError
        else:
          emb = discord.Embed(title="missing permissions", description="you are missing a required role to use this command", color=discord.Color.red())
      else:
        emb = discord.Embed(title="command disabled", description="this command has been disabled by a dev")
      if dont_send == False:
        await ctx.send(embed=emb)



def setup(bot):
  bot.add_cog(moderationCog(bot))