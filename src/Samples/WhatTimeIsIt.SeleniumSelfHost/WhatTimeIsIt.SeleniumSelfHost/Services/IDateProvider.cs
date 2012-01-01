using System;

namespace WhatTimeIsIt.SeleniumSelfHost.Services
{
    public interface IDateProvider
    {
        DateTime CurrentDate { get; }
    }
}