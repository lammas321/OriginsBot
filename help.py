from discord.ext import commands
import discord
import json as js


class helpCog(commands.Cog):
  def __init__(self, bot):
    self.bot = bot

  @commands.command(name="help")
  @commands.cooldown(rate=1, per=5, type=commands.BucketType.user)
  async def help(self, ctx, Command=None):
    with open("./db/CogsConfig.json", "r") as cogConfig:
      cogConfig = js.load(cogConfig)

      cogmands = cogConfig["commands"]

      for command in cogmands:
        if command["name"] == "help":
          if command["disabled"] != True:
            if Command == None:
              emb = discord.Embed(title="commands", description="all public bot commands", color=discord.Color.dark_blue())
              general = " "
              moderation = " "
              setup = " "
              for command in cogmands:
                if command["Type"]== "general":
                  general += "`{0}`, ".format(command["name"])
                
                elif command["Type"] == "moderation":
                  moderation += "`{0}`, ".format(command["name"])
                elif command["Type"] == "setup":
                  setup += "`{0}`, ".format(command["name"])

              emb.add_field(name="general", value=general)
              emb.add_field(name="moderation", value=moderation)
              if setup == " ":
                setup = "none"
              emb.add_field(name="setup", value=setup)
              emb.set_footer(text="a red embed means the command will not work")
            else:
              found = False
              for command in cogmands:
                if command["name"] == Command.lower():
                  found = True
                  command_desc = command["description"]
                  disabled = command["disabled"]
                  syntax = command["syntax"]

                  if disabled == True:
                    color = discord.Color.red()
                  else:
                    color = discord.Color.green()
                  
                  emb = discord.Embed(title=Command, description=command_desc, color=color)
                  emb.add_field(name="syntax", value=syntax)
                  emb.set_footer(text="command_disabled={0}".format(disabled))

                if found == False:
                  emb = discord.Embed(title="command not found", description="this command is not found", color=discord.Color.orange())
                  
          else:
            emb = discord.Embed(title="command is currently disabled",color=discord.Color.red())
      
      await ctx.send(embed=emb)

def setup(bot):
  bot.add_cog(helpCog(bot))