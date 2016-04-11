PLUGIN.Title = "Glue"
PLUGIN.Version = V(1, 0, 1)
PLUGIN.Description = ""
PLUGIN.Author = "Bombardir"
PLUGIN.HasConfig = true
PLUGIN.ResourceId = 733

local msgs, mode = {}, {}
local function SendMessage(player, msg)
	player:SendConsoleCommand("chat.add \"".. msgs.ChatName.."\" \"".. msg .."\"")
end 
local function GetData(player)
	local data = mode[player] or {}
	mode[player] = data
	return data
end
local function GetBone(hitinfo)
	local array = util.TableToArray( { 0 })
	util.ConvertAndSetOnArray(array, 0, hitinfo.HitBone or 0, System.UInt32._type)
	return global.StringPool.Get.methodarray[0]:Invoke(nil, array )
end 
function PLUGIN:Init()
	self.Config.Admin_Auth_LvL = self.Config.Admin_Auth_LvL or 2
	self.Config.Chat_Command   = self.Config.Chat_Command or "glue"
	
	msgs = self.Config.Messages or {}
	msgs.NoPerm = msgs.NoPerm or "No Permission!"
	msgs.ChatName = msgs.ChatName or "[Glue]"
	msgs.NoParent = msgs.NoParent or "This entity has no parent."
	msgs.Already = msgs.Already or "This entity has already been added!"
	msgs.BoneNot = msgs.BoneNot or "Bone not found."
	msgs.NoParentFound = msgs.NoParentFound or "Entities or players with this name not found!"
	msgs.SyntaxError = msgs.SyntaxError or "Syntax Error! /%s m [mode_num] (1 - disable/clear selected, 2 - entity to glue, 3 - parent entity to glue, 4 - unglue, 5 - get bone name)"
	msgs.SyntaxError2 = msgs.SyntaxError2 or "Syntax Error! /%s [entity/player] [parent entity/player]"
	msgs.mode = msgs.mode or {}
	msgs.mode[1] = msgs.mode[1] or "Glue mode disabled."
	msgs.mode[2] = msgs.mode[2] or {"Select the entities/entity to glue.", "Entity added to the glue list."}
	msgs.mode[3] = msgs.mode[3] or {"Select the parent entity to glue.", "Entities glued!" }
	msgs.mode[4] = msgs.mode[4] or {"Select the entity to unglue.", "Entities unglued!" }
	msgs.mode[5] = msgs.mode[5] or "Shoot entity to get bone name."
	self.Config.Messages = msgs
	self:SaveConfig() 
	
	command.AddChatCommand(self.Config.Chat_Command, self.Plugin, "C_Glue")
end

function PLUGIN:C_Glue(player, _, args)
	if player:GetComponent("BaseNetworkable").net.connection.authLevel >= self.Config.Admin_Auth_LvL then
		if args.Length > 0 then
			local arg_0 = args[0]
			if arg_0 == "m" then
				local arg = tonumber(args[1]) or 1
				if arg > 0 and arg < 6 then
					local data = GetData(player)
					if arg == 1 then
						data.mode = nil
						data.ents = nil
					else
						data.ents = data.ents or {}
						data.mode = arg
					end
					SendMessage(player, msgs.mode[arg][1] or msgs.mode[arg])
				else
					SendMessage(player, msgs.SyntaxError:format(self.Config.Chat_Command))
				end 
			else
				if args.Length > 1 then 
					local P_need_to_spawn = false
					local parent = global.BasePlayer.Find(args[1])
					if not parent then
						parent = global.GameManager.CreateEntity(args[1], player.transform.position, player.transform.rotation)
						P_need_to_spawn = true
					end
					if parent then
						local T_need_to_spawn = false
						local target = global.BasePlayer.Find(arg_0)
						if not target then
							target = global.GameManager.CreateEntity(arg_0, parent.transform.position, parent.transform.rotation)
							T_need_to_spawn = true
						end
						if target then
							if P_need_to_spawn then parent:Spawn(true) end
							if T_need_to_spawn then target:Spawn(true) end
							target:SetParent(parent, '')
							SendMessage(player, msgs.mode[3][2])
						else
							SendMessage(player, msgs.NoParentFound)
						end
					else
						SendMessage(player, msgs.NoParentFound)
					end
				else
					SendMessage(player, msgs.SyntaxError2:format(self.Config.Chat_Command))
				end
			end
		end
	else
		SendMessage(player, msgs.NoPerm)
	end
end

function PLUGIN:OnPlayerAttack(player, hitinfo)
	local ent = hitinfo.HitEntity
	if ent then
		local data = GetData(player)
		if data.mode then
			if data.mode == 5 then
				local bone = GetBone(hitinfo)
				if bone == "" then bone = msgs.BoneNot end
				SendMessage(player, bone)
			else
				if data.mode == 2 then
					for i=1, #data.ents do
						if data.ents[i] == ent then
							SendMessage(player, msgs.Already)
							return true 
						end
					end
					table.insert(data.ents, ent)
				elseif data.mode == 3 then
					local bone = GetBone(hitinfo)
					for i=1, #data.ents do
						data.ents[i]:SetParent(ent, bone)
					end
				elseif data.mode == 4 then
					if ent:GetParentEntity() then
						ent:SetParent(nil, "")
					else
						SendMessage(player, msgs.NoParent)
						return true
					end
				end
				SendMessage(player, msgs.mode[data.mode][2])
			end
			return true
		end
	end
end