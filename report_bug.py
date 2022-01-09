from discord.ext import commands
import discord
import json as js
import os
def check(author):
  def inner(message):
    return message.author == author
  return inner

class reportCog(commands.Cog):
  def __init__(self, bot):
    self.bot = bot
    self.db = "./db/"

  @commands.command(name="report-bug")
  async def report_bug(self, ctx, *bug):


    await ctx.send(f"{ctx.author.mention} please give this bug a title")
    title = await self.bot.wait_for('message', check=check, timeout=30)

    bug = {"desc": bug, "reporter": ctx.author.name, "id": ctx.author.id}
    try:
      with open("/home/runner/OriginsBot/cogs/db/bugs/{0}".format(title.content.lower()), "x") as nbug:
        js.dump(bug, nbug)
    except FileExistsError:
      await ctx.send("a bug with this name already exists try `.view-bugs`")

  @commands.command(name="view-bugs")
  async def view_bugs(self, ctx):
    emb = discord.Embed(title="active bugs", description="these are the active bug reports", color=discord.Color.blurple())
    bugs = []
    for file in os.listdir("./db/bugs/"):
      bugs.append(file)
    
    for i in range(len(bugs)):
      emb.add_field(name=bugs[i], value="id: {0}".format(i+1))

    await ctx.send(embed=emb)

    await ctx.send("say the ID or name of the bug you want to view")
    bug = await self.bot.wait_for("message", check=check, timeout=30)

    try:
      bug = int(bug.content)
      file = bugs[bug-1]

      with open("./cogs/db/bugs/{0}".format(file), "r") as bug:
        bug = js.load(bug)

      desc = " ".join(bug)
      emb = discord.Embed(title=file, description=desc, color=discord.Color.orange())
  
    except ValueError:
      for Bug in bugs:
        if Bug == bug:
          with open("./db/bugs/{0}".format(file), "r") as bug:
            bug = js.load(bug)
          
          desc = " ".join(bug)
          emb = discord.Embed(title=bug, description=desc, color=discord.Color.orange())
    
    await ctx.send(emb)
          

def setup(bot):
  bot.add_cog(reportCog(bot))