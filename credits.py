from discord.ext import commands
import discord

class CreditsCog(commands.Cog):
  def __init__(self, bot):
    self.bot = bot
  
  @commands.command(name="credits")
  async def credits(self, ctx):
    emb = discord.Embed(title="credits", description="the people who worked on or made this project possible", color=discord.Color.dark_blue())
    emb.add_field(name="Programming", value="Karsonthefoxx#9074")
    emb.add_field(name="formatting storage files", value="Karsonthefoxx#9074, lammas123#6714")
    emb.add_field(name="sources", value="lammas123#6714")
    emb.add_field(name="bot art and other images", value="lammas123#6714")

    await ctx.send(ctx.author.mention, embed=emb)

def setup(bot):
  bot.add_cog(CreditsCog(bot))