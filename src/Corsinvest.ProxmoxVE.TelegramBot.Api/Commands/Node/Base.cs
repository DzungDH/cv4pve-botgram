﻿/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: 2019 Copyright Corsinvest Srl
 */

using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.TelegramBot.Api;
using Corsinvest.ProxmoxVE.TelegramBot.Commands.Api;
using Corsinvest.ProxmoxVE.TelegramBot.Helpers.Api;
using Telegram.Bot.Types;

namespace Corsinvest.ProxmoxVE.TelegramBot.Commands.Node.Api;

internal abstract class Base : Command
{
    protected abstract bool ForReboot { get; }

    public override async Task<bool> Execute(Message message, BotManager botManager)
    {
        await botManager.BotClient.ChooseNodeInlineKeyboard(message.Chat.Id, await botManager.GetPveClientAsync(), true);
        return false;
    }

    public override async Task<bool> Execute(Message message, CallbackQuery callbackQuery, BotManager botManager)
    {
        var action = ForReboot ? "reboot" : "shutdown";
        await (await botManager.GetPveClientAsync()).Nodes[callbackQuery.Data].Status.NodeCmd(action);

        await botManager.BotClient.SendTextMessageAsyncNoKeyboard(callbackQuery.Message.Chat.Id,
                                                                  $"Node {callbackQuery.Data} action {action}!");

        return true;
    }
}