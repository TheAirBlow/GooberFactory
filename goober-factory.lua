-- Add this script to your save archive's root folder (is the same as the filename, e.g. gamesave)
-- And add the following into the control.lua file: handler.add_lib(require("goober-factory"))
local version = '2.0'; -- Goober's Factory integration by TheAirBlow

local function on_rocket_launched(event)
	localised_print({"", "0000-00-00 00:00:00 [ROCKET] " .. event.rocket.last_user.name .. " launched a rocket!"})
end

local function on_pre_player_died(event)
	local player = game.get_player(event.player_index)
	if event.cause ~= nil and event.cause.type == "character" then
		local cause = game.get_player(event.cause.player.index)
		if player ~= nil then
			localised_print({"", "0000-00-00 00:00:00 [DIED] "  .. player.name .. " was killed by " .. cause.name, " at " .. tostring(math.floor(player.position.x)) .. " " .. tostring(math.floor(player.position.y)) .. "!"})
		else
			localised_print({"", "0000-00-00 00:00:00 [DIED] " .. player.name .. " died from natural causes at " .. tostring(math.floor(player.position.x)) .. " " .. tostring(math.floor(player.position.y)) .. "!"})
		end
	elseif event.cause ~= nil then
		localised_print({"", "0000-00-00 00:00:00 [DIED] " .. player.name .. " was killed by ", event.cause.localised_name, " at " .. tostring(math.floor(player.position.x)) .. " " .. tostring(math.floor(player.position.y)) .. "!"})
	else
		localised_print({"", "0000-00-00 00:00:00 [DIED] " .. player.name .. " died from natural causes at " .. tostring(math.floor(player.position.x)) .. " " .. tostring(math.floor(player.position.y)) .. "!"})
	end
end

local reasons_localized = {
	[5] = "because their internet connection couldn't keep up",
	[4] = "because of desync limit being reached",
	[8] = "because of being kicked and deleted",
	[10] = "because of switching servers",
	[2] = "because they're reconnecting",
	[3] = "because of an invalid input",
	[1] = "because of being dropped",
	[6] = "because of being AFK",
	[7] = false,
	[9] = false,
	[0] = false
}

local function on_player_left_game(event)
	local player = game.get_player(event.player_index)
	local localized = reasons_localized[event.reason]
	if localized ~= false then
		localised_print({"", "0000-00-00 00:00:00 [DISCONNECT] " .. player.name .. " disconnected " .. localized})
	end
end

local function on_player_respawned(event)
	local player = game.get_player(event.player_index)
	if player ~= nil then
		localised_print({"", "0000-00-00 00:00:00 [RESPAWN] " .. player.name .. " respawned!"})
	end
end

local function on_research_finished(event)
	if event.research.level ~= nil then
		localised_print({"", "0000-00-00 00:00:00 [RESEARCH] Finished ", event.research.localised_name, "!"})
	else
		localised_print({"", "0000-00-00 00:00:00 [RESEARCH] Finished ", event.research.localised_name, "!"})
	end
end

local function on_research_started(event)
	if event.last_research == nil then
		localised_print({"", "0000-00-00 00:00:00 [RESEARCH] Started research for ", event.research.localised_name, "!"})
	else
        localised_print({"", "0000-00-00 00:00:00 [RESEARCH] Research changed from ", event.last_research.localised_name, " to ", event.research.localised_name, "!"})
	end
end

local lib = {}

lib.events = {
	[defines.events.on_research_finished] = on_research_finished,
	[defines.events.on_research_started] = on_research_started,
	[defines.events.on_player_left_game] = on_player_left_game,
	[defines.events.on_player_respawned] = on_player_respawned,
	[defines.events.on_rocket_launched] = on_rocket_launched,
	[defines.events.on_pre_player_died] = on_pre_player_died,
}

lib.on_event = function(event)
  local action = events[event.name]
  if not action then return end
  return action(event)
end

-- GFI stands for Goober's Factory Integration
commands.add_command("gfi-print", "prints a message to chat (console only)", function(command)
  if command.player_index ~= nil then
    game.get_player(command.player_index).print("This command can only be ran from the server's console!")
  else
    game.print(command.parameter)
  end
end)

commands.add_command("gfi-list", "prints list of online players in a more friendly format (console only)", function(command)
  if command.player_index ~= nil then
    game.get_player(command.player_index).print("This command can only be ran from the server's console!")
  else
    local str = ""
    local total = 0
    for _, player in ipairs(game.connected_players) do
		str = str .. player.name .. ", "
		total = total + 1
	end
	if string.len(str) ~= 0 then
		str = string.sub(str, 0, -3)
	end
    print("0000-00-00 00:00:00 [LIST] Players online: " .. str)
  end
end)

commands.add_command("gfi-version", "prints GFI version (console only)", function(command)
  if command.player_index ~= nil then
    game.get_player(command.player_index).print("This command can only be ran from the server's console!")
  else
    print("0000-00-00 00:00:00 [GFI] Goober's Factorio Integration version " .. version)
  end
end)

return lib
