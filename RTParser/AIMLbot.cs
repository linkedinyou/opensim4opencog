﻿using System;
using System.Collections.Generic;
using System.Text;
using RTParser;
using RTParser.Utils;

namespace AIMLbot
{
    public class Bot : RTParser.RTPBot
    {
         
        public Bot()
            : base()
        {
        }
        public void loadAIMLFromFiles()
        {
            base.loadAIMLFromDefaults();
        }
    }
    public class User : RTParser.User
    {
        public User(string UserID, Bot bot)
            : base(UserID, bot)
        {
        }
        public User(string UserID, RTPBot bot)
            : base(UserID, bot)
        {
        }
    }
    public class Request : RTParser.RequestImpl
    {/*
        public Request(String rawInput, RTParser.User user, RTPBot bot)
            : this(rawInput, user, bot, null)
        {
        }*/
        public Request(String rawInput, RTParser.User user, RTPBot bot, RTParser.Request r)
            : base(rawInput, user, bot, r, null)
        {
        }
        public Request(String rawInput, RTParser.User user, RTPBot bot, RTParser.Request r, RTParser.User targetUser)
            : base(rawInput, user, bot, r, targetUser)
        {
        }
    }

    public class Result : RTParser.Result
    {
        public Result(RTParser.User user, RTPBot bot, RTParser.Request request, RTParser.Result parent)
            : base(user, bot, request, parent)
        {

        }
    }
    
    namespace Utils
    {
        public class AIMLLoader : RTParser.Utils.AIMLLoader
        {
            public AIMLLoader(RTPBot bot)
                : base(bot, bot == null ? null : bot.GetBotRequest("-AIMLLoader-"))
            {
            }
        }
    }

}
