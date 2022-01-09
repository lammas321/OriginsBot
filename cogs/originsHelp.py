from discord.ext import commands
import discord
import json as js
from os import listdir

def check(author):
  def inner(message):
    return message.author == author
  return inner

class OriginNotFound(Exception):
  pass


class origins_infoCog(commands.Cog):
  def __init__(self, bot):
    self.bot = bot
    self.db = "./db/"
  
  @commands.command(name="origin")
  async def origins_help(self, ctx, origin):
    try:
      with open(self.db+"origins.json", "r") as origins:
        origins = js.load(origins)
      
      found = False

      for Origin in origins["origins"]:
        if Origin["name"] == origin.lower():
          found = True
          emb = discord.Embed(title=origin.capitalize(), description=Origin["description"])

          for power in Origin["powers"]:
            emb.add_field(name=power["name"], value=power["description"], inline=True)
        
      if found == False:
        raise OriginNotFound
    except FileNotFoundError:
      emb = discord.Embed(title="the database seems to be missplaced", description="i cannot find the origins.json")
    except OriginNotFound:
      emb = discord.Embed(title="origin not found", description="check the spelling of the origin and try again", color=discord.Color.orange())
      emb.set_footer(text="The selected origin may not have been added to my database yet")
    

    await ctx.send(embed=emb)




def setup(bot):
  bot.add_cog(origins_infoCog(bot))