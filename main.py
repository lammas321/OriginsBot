from discord.ext import commands
import discord
from os import listdir as ldir, environ as evn

PREFIXS = [".", "!", ">"]

bot = commands.Bot(command_prefix=PREFIXS, help_command=None)


@bot.event
async def on_ready():
    await bot.change_presence(
        activity=discord.Game(".help or !help || server support/moderation"))
    for filename in ldir('./cogs'):
        if filename.endswith('.py'):
          try:
            bot.load_extension(f'cogs.{filename[:-3]}')
          except Exception:
            print("skipped a cog: {0}".format(filename))

    print("setup finished")

#reloads all cogs
@bot.command(name="reload")
async def reload(ctx):
  skipped=[]

  myID = 855948446540496896
  if ctx.author.id == myID:
    for file in ldir("cogs"):
      if file.endswith(".py"):
        try:
          await bot.reload_extension(f"cogs.{file[:-3]}")
          print("reloaded cog: {cog}".format(file))
        except Exception:
          skipped.append(file)

    finished = discord.Embed(title="reload complete",description="all cogs were reloaded",color=discord.Color.green())
    finished.add_field(name="skipped", value=skipped)
    await ctx.send(embed=finished)
  else:
      await ctx.send("you are not a developer")

#error handler
@bot.event
async def on_command_error(ctx, error):
  if isinstance(error, commands.BotMissingPermissions):
    emb = discord.Embed(title="bot missing perms",description="i do not have permission to do this", color=discord.Color.red())

  if isinstance(error, commands.MissingPermissions):
    emb = discord.Embed(title="missing permissions", description="you are missing a required role to use this command", color=discord.Color.red())

  if isinstance(error, commands.CommandInvokeError):
    emb = discord.Embed(title="something went wrong", color=discord.Color.red())

  if isinstance(error, commands.CommandNotFound):
    emb = discord.Embed(title="command not found", description="this command is not found check your spelling", color=discord.Color.red())
    emb.add_field(name="possible?", value="this command may be disabled at the moment")

  if isinstance(error, commands.CommandOnCooldown):
    emb = discord.Embed(title="dang", description="Your on cooldown! try again after {0}s".format(round(error.retry_after, 1)))

  if isinstance(error, commands.MissingRequiredArgument):
    emb = discord.Embed(title="missing argument", description="one or more required argument is missing", color=discord.Color.red())
    await ctx.send(embed=emb)



bot.run('Token :)')
