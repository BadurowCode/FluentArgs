﻿namespace FluentArgs.Execution
{
    using System;
    using System.Threading.Tasks;
    using FluentArgs.Description;

    internal class InitialStep : Step
    {
        public ParserSettings? ParserSettings { get; set; }

        public override Task Execute(State state)
        {
            if (Next == null)
            {
                throw new Exception("TODO: Good message");
                //return Task.CompletedTask;
            }

            return Next.Execute(state);
        }
    }
}
