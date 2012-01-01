using System;

namespace WhatTimeIsIt.SeleniumSelfHost.Services
{
    internal class RealDateProvider : IDateProvider
    {
        public DateTime CurrentDate { get { return DateTime.Now; } }
    }
}