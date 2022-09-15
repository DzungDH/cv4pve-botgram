﻿/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: 2019 Copyright Corsinvest Srl
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.TelegramBot.Helpers.Api;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Corsinvest.ProxmoxVE.TelegramBot.Commands.Api;

internal class Alias : Command
{
    private enum RequestType
    {
        Action,
        CreateRequestName,
        CreateRequestDescription,
        CreateRequestCommand,
        DeleteRequestName
    }

    private RequestType _requestType = RequestType.Action;
    private readonly ApiExplorerHelper.AliasManager _aliasManager;
    private string _name;
    private string _description;

    public Alias()
    {
        _aliasManager = new ApiExplorerHelper.AliasManager
        {

            FileName = Path.Combine(FilesystemHelper.GetApplicationDataDirectory("cv4pve-botgram"), "alias.txt")
        };
        _aliasManager.Load();
    }

    public override string Name => "alias";
    public override string Description => "Create alias commands";

    public override async Task<bool> Execute(Message message,
                                             CallbackQuery callbackQuery,
                                             TelegramBotClient botClient)
    {
        var endCommand = false;
        switch (_requestType)
        {
            case RequestType.Action:
                switch (callbackQuery.Data)
                {
                    case "List":
                        await botClient.SendDocumentAsyncFromText(message.Chat.Id,
                                                                  _aliasManager.ToTable(true, TableGenerator.Output.Html),
                                                                  "alias.html");
                        endCommand = true;
                        break;

                    case "Delete":
                        //request name
                        _requestType = RequestType.DeleteRequestName;
                        await botClient.SendTextMessageAsyncNoKeyboard(message.Chat.Id, "Name alias");
                        break;

                    case "Create":
                        //request name
                        _requestType = RequestType.CreateRequestName;
                        await botClient.SendTextMessageAsyncNoKeyboard(message.Chat.Id, "Name alias");
                        break;

                    default: break;
                }
                break;

            default: break;
        }

        return await Task.FromResult(endCommand);
    }

    public override async Task<bool> Execute(Message message, TelegramBotClient botClient)
    {
        var endCommand = false;

        switch (_requestType)
        {
            case RequestType.Action:
                await botClient.ChooseInlineKeyboard(message.Chat.Id, "Choose action", new[] { "List", "Delete", "Create" });
                break;

            case RequestType.DeleteRequestName:
                if (_aliasManager.Exists(message.Text))
                {
                    _aliasManager.Remove(message.Text);
                }
                else
                {
                    await botClient.SendTextMessageAsyncNoKeyboard(message.Chat.Id, $"Name not valid '{message.Text}'");
                }
                endCommand = true;
                break;

            case RequestType.CreateRequestName:
                _name = message.Text.Trim();
                if (_aliasManager.Exists(_name))
                {
                    await botClient.SendTextMessageAsyncNoKeyboard(message.Chat.Id, $"Name not valid '{_name}'");
                    endCommand = true;
                }
                else
                {
                    await botClient.SendTextMessageAsyncNoKeyboard(message.Chat.Id, "Description");
                    _requestType = RequestType.CreateRequestDescription;
                }
                break;

            case RequestType.CreateRequestDescription:
                _description = message.Text.Trim();
                await botClient.SendTextMessageAsyncNoKeyboard(message.Chat.Id, "Command");
                _requestType = RequestType.CreateRequestCommand;
                break;

            case RequestType.CreateRequestCommand:
                _aliasManager.Create(_name, _description, message.Text.Trim(), false);

                await botClient.SendTextMessageAsyncNoKeyboard(message.Chat.Id, "Command created!");
                _aliasManager.Save();
                endCommand = true;
                break;

            default: break;
        }

        return await Task.FromResult(endCommand);
    }

    public IReadOnlyList<AliasCommand> GetCommandsFromAlias()
        => _aliasManager.Alias.Select(a => new AliasCommand(a)).ToList().AsReadOnly();
}