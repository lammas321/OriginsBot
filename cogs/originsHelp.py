from discord.ext import commands
import discord
import json as js
from os import listdir

def check(author):
  def inner(message):
    return message.author == author
  return inner

def get_color(origin, colors):
  color = colors[origin["color"]]
  return color

class OriginNotFound(Exception):
  pass

#colors
colors = {
  "red": discord.Color.red(),
  "blue": discord.Color.blue(),
  "grey": discord.Color.greyple(),
  "orange": discord.Color.orange(),
  "white": 0xeeffee,
  "blue": discord.Color.blue(),
  "dark_grey": discord.Color.dark_grey(),
  "dark_orange": discord.Color.dark_orange(),
  "dark_blue": discord.Color.dark_blue(),
  "black": discord.Color.from_rgb(0, 0, 0),
  "dark_purple": discord.Color.dark_purple(),
  "purple": discord.Color.purple()
}
#setup cog
class origins_infoCog(commands.Cog):
  def __init__(self, bot):
    self.bot = bot
    self.db = "./db/"
  
  @commands.command(name="origin")
  async def origins_help(self, ctx, origin):
    try:
      with open(self.db+"origins.json", "r") as origins:
        origins = js.load(origins)
      
      if origin.lower() in origins.keys():
        origin = origins[origin.lower()]
        emb = discord.Embed(title=origin["name"], description=origin["description"], color=get_color(origin, colors))
        
        origin_icon = discord.File(origin["icon"], filename="originicon.png")
        emb.set_thumbnail(url="attachment://originicon.png")

        for power in origin["powers"]:
          emb.add_field(name=power["name"], value=power["description"], inline=True)
        
        await ctx.send(file=origin_icon, embed=emb)
      else:
        raise OriginNotFound

    except FileNotFoundError:
      emb = discord.Embed(title="the database seems to be missplaced", description="i cannot find the origins.json")
      await ctx.send(embed=emb)
    except OriginNotFound:
      emb = discord.Embed(title="origin not found", description="check the spelling of the origin and try again", color=discord.Color.orange())
      emb.set_footer(text="The selected origin may not have been added to my database yet")
      await ctx.send(embed=emb)

def setup(bot):
  bot.add_cog(origins_infoCog(bot))
