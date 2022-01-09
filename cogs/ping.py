from discord.ext import commands
import discord

class pingCog(commands.Cog):
  def __init__(self, bot):
    self.bot = bot

  @commands.command(name="ping")
  async def ping(self, ctx):
    print("ping")
    emb = discord.Embed(title="pong", color=discord.Color.green())
    emb.add_field(name="latency", value="{0}ms".format(round(self.bot.latency*100)))

    await ctx.send(embed=emb)

def setup(bot):
  bot.add_cog(pingCog(bot))