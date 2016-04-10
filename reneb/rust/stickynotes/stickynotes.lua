PLUGIN.Name = "Sticky Notes"
PLUGIN.Title = "Sticky Notes"
PLUGIN.Version = V(1, 1,1)
PLUGIN.Description = "Place sticky notes for other players to read"
PLUGIN.Author = "Reneb"
PLUGIN.HasConfig = true

local DataFile = "stickynotes"
local NotesData = {}
local PlayersData = {}
local RadiationZonesNote = {}
 
function PLUGIN:Init()
	command.AddChatCommand( "note", self.Plugin, "cmdNote" )
	command.AddChatCommand( "note_reset", self.Plugin, "cmdNoteReset" )
	command.AddChatCommand( "note_count", self.Plugin, "cmdNoteCount" )
	command.AddChatCommand( "note_read", self.Plugin, "cmdNoteRead" )
	--self.Config = {}
	--self:LoadDefaultConfig()
	self:LoadDataFile()
	
	
end
function PLUGIN:OnServerInitialized()
	pcall(new, UnityEngine.Vector3._type, nil)
    pcall(new, UnityEngine.Quaternion._type , nil)
	newpos = new( UnityEngine.Vector3._type , nil )
	newrot = new( UnityEngine.Quaternion._type , nil )
	self:InitNotes()
	
end
function PLUGIN:LoadDefaultConfig()
	self.Config.Settings = {}
	self.Config.Settings.authLevel = 1
	self.Config.Settings.messageName = "Sticky-Note"
	
	self.Config.Messages = {}
	self.Config.Messages.NoPermissions = "You do not have the permission to use this command"
	self.Config.Messages.NoMoreStickyNotesLeft = "You are not allowed to add any other sticky notes at the moment"
	self.Config.Messages.WrongNoteArgument1 = "You must add a message after /note"
	self.Config.Messages.NoMessageSet = "You didnt put any message"
	self.Config.Messages.NoSourcePlayer = "No players set as adding the note"
	self.Config.Messages.SuccessfullyAddedTheStickyNote = "You successfully added the sticky note"
	self.Config.Messages.YouMustSpecifyASteamID = "You must specify a steamID"
	self.Config.Messages.NewMessage = "You've got a new message from: "
	self.Config.Messages.MessageAutoDestroyed = "This sticky note was auto destroyed"
	self.Config.Messages.NotesDeletedAroundPos = " notes were deleted around your position"
	self.Config.Messages.NoNotesAroundYou = "No notes were found around your position"
	self.Config.Messages.SuccessfullyResetNotes = "Successfully resetted all notes"
	self.Config.Messages.CountNotes = " total notes deployed"
	
	self.Config.Messages.NoteCMD0 = "/note \"Message\" - to add a sticky note where you are for players to see when they come here"
	self.Config.Messages.NoteCMD1 = "/note - to remove sticky notes where you are standing at"
	self.Config.Messages.NoteCMDAdmin0 = "/note_reset - to reset all notes"
	self.Config.Messages.NoteCMDAdmin1 = "/note_count - to see how many notes are around the map"
	self.Config.Messages.NoteCMDAdmin2 = "/note_read RADIUS - Read all notes around you in the radius (default is 30m)"
	self.Config.StickyNotes = {}
	self.Config.StickyNotes.zoneRadius = 2
	self.Config.StickyNotes.timeBeforeDestroy = 86400 -- 1 day
	self.Config.StickyNotes.maxStickyNotesPerPlayer = 5
	self.Config.StickyNotes.levelForCommandUsage = 0
	self.Config.StickyNotes.overRideLimitAuthLevel = 1
	self.Config.StickyNotes.anonymous = false
end
local function newTriggerBase(x,y,z,rad)
	trigger = UnityEngine.Object.FindObjectsOfTypeAll(global.TriggerRadiation._type)
	
	newgameobj = new( UnityEngine.GameObject._type , nil )
	newpos = newgameobj:GetComponent(UnityEngine.Transform._type).position
	newgameobj.layer = UnityEngine.LayerMask.NameToLayer("Trigger")
	newpos.x = x
	newpos.y = y
	newpos.z = z
	newgameobj.name = "Sticky Notes"
	newgameobj:GetComponent(UnityEngine.Transform._type).position = newpos
	newgameobj:AddComponent(UnityEngine.SphereCollider._type)
	newgameobj:GetComponent(UnityEngine.SphereCollider._type).radius = rad
	newgameobj:SetActive(true);
	newgameobj:AddComponent(global.TriggerBase._type)
	newgameobj:GetComponent(global.TriggerBase._type).interestLayers = trigger[trigger.Length-1]:GetComponent(global.TriggerBase._type).interestLayers
	return newgameobj:GetComponent(global.TriggerBase._type)
end 

function PLUGIN:LoadDataFile()
    local data = datafile.GetDataTable("stickynotes")
    NotesData = data or {}
end
function PLUGIN:SaveData()
    datafile.SaveDataTable("stickynotes")
end
local function Distance2D(p1, p2)
    return math.sqrt(math.pow(p1.x - p2.x,2) + math.pow(p1.z - p2.z,2)) 
end
local function GetFreeID(steamID)
	if(not NotesData[steamID]) then NotesData[steamID] = {} return 1 end
	for i=1,10000 do
		if(not NotesData[steamID][tostring(i)]) then
			return i
		end
	end
	return false
end

local function addPlayerZone(steamid,zone)
	if(not PlayersData[steamid]) then PlayersData[steamid] = {} end
	PlayersData[steamid][zone] = true
end
local function createZone(steamID,freeid,data)
	newpos.x = data.position.x
	newpos.y = data.position.y
	newpos.z = data.position.z
	local newBaseEntity = newTriggerBase(data.position.x, data.position.y, data.position.z, self.Config.StickyNotes.zoneRadius)
	if(not newBaseEntity) then
		print("Error while making a sticky note, couldn't create a new zone")
		return false
	end
	RadiationZonesNote[newBaseEntity] = {
					name=data.from,
					message=data.msg,
					origin=steamID,
					started=data.addTime,
					originid=tostring(freeid)
				}
	addPlayerZone(steamID,newBaseEntity)
				
end
local function isZone(steamID,freeid,data)
	allRadiationZone = UnityEngine.Object.FindObjectsOfTypeAll(global.TriggerBase._type)
	for i=0, allRadiationZone.Length-1 do
		if(allRadiationZone[i].gameObject.name == "Sticky Notes") then
			if(allRadiationZone[i]:GetComponent(UnityEngine.Transform._type).transform.position.x == data.position.x and allRadiationZone[i]:GetComponent(UnityEngine.Transform._type).transform.position.z == data.position.z) then
				RadiationZonesNote[allRadiationZone[i]] = {
					name=data.from,
					message=data.msg,
					origin=steamID,
					started=data.addTime,
					originid=tostring(freeid)
				}
				addPlayerZone(steamID,allRadiationZone[i])
				return true
			end
		end
	end
	return false
end
function PLUGIN:removeNotesByPos(pos)
	count = 0
	allRadiationZone = UnityEngine.Object.FindObjectsOfTypeAll(global.TriggerBase._type)
	for i=0, allRadiationZone.Length-1 do
		if(allRadiationZone[i].gameObject.name == "Sticky Notes") then
			if(RadiationZonesNote[allRadiationZone[i]]) then
				if(Distance2D(allRadiationZone[i]:GetComponent(UnityEngine.Transform._type).transform.position,pos) <= self.Config.StickyNotes.zoneRadius) then
					count = count + 1
					allRadiationZone[i]:RemoveObject(allRadiationZone[i].gameObject)
					NotesData[RadiationZonesNote[allRadiationZone[i]].origin][RadiationZonesNote[allRadiationZone[i]].originid] = nil
					PlayersData[RadiationZonesNote[allRadiationZone[i]].origin][RadiationZonesNote[allRadiationZone[i]]] = nil
					RadiationZonesNote[allRadiationZone[i]] = nil
				end
			end
		end
	end
	self:SaveData()
	return count
end
local function removeZone(steamID,freeid)
	allRadiationZone = UnityEngine.Object.FindObjectsOfTypeAll(global.TriggerBase._type)
	for i=0, allRadiationZone.Length-1 do
		if(allRadiationZone[i].gameObject.name == "Sticky Notes") then
			if(RadiationZonesNote[allRadiationZone[i]]) then
				if(RadiationZonesNote[allRadiationZone[i]].origin == steamID and RadiationZonesNote[allRadiationZone[i]].originid == freeid) then
					allRadiationZone[i]:RemoveObject(allRadiationZone[i].gameObject)
					RadiationZonesNote[allRadiationZone[i]] = nil
					return true
				end
			end
		end
	end
	return false
end
function PLUGIN:InitNotes()
	for steamid, data in pairs(NotesData) do
		for i, zonedata in pairs(data) do
			if(not isZone(steamid,i,zonedata)) then
				createZone(steamid,i,zonedata)
			end
		end
	end
end
function PLUGIN:addZone(steamid,pos,zoneradius,name,message)
	freeid = GetFreeID(steamid)
	NotesData[steamid][tostring(freeid)] = {
		from=name,
		msg=message,
		position = {
			x=pos.x,
			y=pos.y,
			z=pos.z
		},
		zradius=tonumber(zoneradius),
		addTime=time.GetUnixTimestamp()
	}
	createZone(steamid,freeid,NotesData[steamid][tostring(freeid)])
	self:SaveData()
	return true
end

function PLUGIN:countNotes(steamID)
	if(not PlayersData[steamID]) then return 0 end
	count = 0
	for zone, s in pairs(PlayersData) do
		count = count + 1
	end
	return count
end
function PLUGIN:tryAddNote(player,message)
	name = "Anonymous"
	if(not player or not player.displayName) then
		return false, self.Config.Messages.NoSourcePlayer
	end
	if(not self.Config.StickyNotes.anonymous) then
		name = player.displayName
	end
	if(message == nil or message == "" or message == " ") then
		return false, self.Config.Messages.NoMessageSet
	end
	steamid = rust.UserIDFromPlayer(player)
	if(self:countNotes(steamid) >= self.Config.StickyNotes.maxStickyNotesPerPlayer and player:GetComponent("BaseNetworkable").net.connection.authLevel < self.Config.StickyNotes.overRideLimitAuthLevel) then
		return false, self.Config.Messages.NoMoreStickyNotesLeft
	end
	success, err = self:addNote(steamid,player.transform.position,self.Config.StickyNotes.zoneRadius,name,message)
	if(not success) then
		return false, err
	end
	return success
end
function PLUGIN:removeNote(steamid,freeid)
	if(tonumber(freeid) == nil) then
		print("Error while trying to remove a sticky note from: " .. steamid)
		return false
	end
	if(tonumber(steamid) == nil) then
		print("Error while trying to remove a sticky note, couldn't get the steamID")
		return false
	end
	freeid = tostring(freeid)
	success = removeZone(steamid,freeid)
	NotesData[steamid][freeid] = nil
	if(not success) then
		print("No zones found while trying to remove note from " .. steamid .. " n° ".. freeid)
		return false
	end
	return true
end
function PLUGIN:addNote(steamid,position,zradius,name,message)
	if(not steamid) then steamid = 10000000000000000 end
	if(not name) then name = "Anonymous" end
	if(not message) then return false, self.Config.Messages.NoMessageSet end
	if(tonumber(zradius) == nil) then return false, "Radius needs to be a number" end
	self:addZone(steamid,position,zradius,name,message)
	return self.Config.Messages.SuccessfullyAddedTheStickyNote
end
function PLUGIN:cmdNoteReset(player,cmd,args)
	if(player:GetComponent("BaseNetworkable").net.connection.authLevel < self.Config.Settings.authLevel) then
		rust.SendChatMessage(player,self.Config.Settings.messageName,self.Config.Messages.NoPermissions)
		return
	end
	for k,v in pairs(NotesData) do
		for i,u in pairs(v) do
			removeZone(k,i)
			NotesData[k][i] = nil
		end
		NotesData[k] = nil
	end
	NotesData = {}
	self:SaveData()
	rust.SendChatMessage(player,self.Config.Settings.messageName,self.Config.Messages.SuccessfullyResetNotes)
end
function PLUGIN:cmdNoteCount(player,cmd,args)
	if(player:GetComponent("BaseNetworkable").net.connection.authLevel < self.Config.Settings.authLevel) then
		rust.SendChatMessage(player,self.Config.Settings.messageName,self.Config.Messages.NoPermissions)
		return
	end
	count = 0
	for k,v in pairs(PlayersData) do
		for i,u in pairs(v) do
			count = count+1
		end
	end
	rust.SendChatMessage(player,self.Config.Settings.messageName,count .. self.Config.Messages.CountNotes)
end
function PLUGIN:cmdNoteRead(player,cmd,args)
	if(player:GetComponent("BaseNetworkable").net.connection.authLevel < self.Config.Settings.authLevel) then
		rust.SendChatMessage(player,self.Config.Settings.messageName,self.Config.Messages.NoPermissions)
		return
	end
	dist = 30
	if(args.Length >= 1) then
		if(tonumber(args[0]) ~= nil) then
			dist = tonumber(args[0])
		end
	end
	allRadiationZone = UnityEngine.Object.FindObjectsOfTypeAll(global.TriggerBase._type)
	for i=0, allRadiationZone.Length-1 do
		if(allRadiationZone[i].gameObject.name == "Sticky Notes") then
			if(RadiationZonesNote[allRadiationZone[i]]) then
				if(Distance2D(allRadiationZone[i]:GetComponent(UnityEngine.Transform._type).transform.position,player.transform.position) <= dist) then
					rust.SendChatMessage(player,self.Config.Settings.messageName,math.ceil(Distance2D(allRadiationZone[i]:GetComponent(UnityEngine.Transform._type).transform.position,player.transform.position)*10)/10 .. "m - " .. RadiationZonesNote[allRadiationZone[i]].origin .. " - " .. RadiationZonesNote[allRadiationZone[i]].originid .. " - " .. RadiationZonesNote[allRadiationZone[i]].message)
				end
			end
		end
	end
end
function PLUGIN:cmdNote(player,cmd,args)
	if(player:GetComponent("BaseNetworkable").net.connection.authLevel < self.Config.StickyNotes.levelForCommandUsage) then
		rust.SendChatMessage(player,self.Config.Settings.messageName,self.Config.Messages.NoPermissions)
		return
	end
	if(args.Length == 0) then
		rust.SendChatMessage(player,self.Config.Settings.messageName,self.Config.Messages.WrongNoteArgument1)
		success = self:removeNotesByPos(player.transform.position)
		if(success > 0) then
			rust.SendChatMessage(player,self.Config.Settings.messageName,success .. self.Config.Messages.NotesDeletedAroundPos)
		else
			rust.SendChatMessage(player,self.Config.Settings.messageName,self.Config.Messages.NoNotesAroundYou)
		end
		return
	end
	msg = ""
	for i=0,args.Length-1 do
		msg = msg .. args[i] .. " "
	end
	success, err = self:tryAddNote(player,msg)
	if(not success) then
		rust.SendChatMessage(player,self.Config.Settings.messageName,err)
		return
	end
end

function PLUGIN:OnEntityEnter(triggerbase,entity)
	if(entity:GetComponentInParent(global.BasePlayer._type)) then
		if(RadiationZonesNote[triggerbase]) then
			rust.SendChatMessage(entity,self.Config.Settings.messageName,self.Config.Messages.NewMessage .. tostring(RadiationZonesNote[triggerbase].name))
			rust.SendChatMessage(entity,self.Config.Settings.messageName,tostring(RadiationZonesNote[triggerbase].message))
			if( (time.GetUnixTimestamp() - RadiationZonesNote[triggerbase].started) >=  self.Config.StickyNotes.timeBeforeDestroy ) then
				triggerbase:RemoveObject(triggerbase.gameObject)
				rust.SendChatMessage(entity,self.Config.Settings.messageName,self.Config.Messages.MessageAutoDestroyed)
				NotesData[RadiationZonesNote[triggerbase].origin][RadiationZonesNote[triggerbase].originid] = nil
				PlayersData[RadiationZonesNote[triggerbase].origin][RadiationZonesNote[triggerbase]] = nil
				RadiationZonesNote[triggerbase] = nil
				self:SaveData()
			end
		end
	end
end

function PLUGIN:SendHelpText(player)
	rust.SendChatMessage(player,self.Config.Settings.messageName,self.Config.Messages.NoteCMD0)
	rust.SendChatMessage(player,self.Config.Settings.messageName,self.Config.Messages.NoteCMD1)
    if(player:GetComponent("BaseNetworkable").net.connection.authLevel >= self.Config.Settings.authLevel) then
    	rust.SendChatMessage(player,self.Config.Settings.messageName,self.Config.Messages.NoteCMDAdmin0)
    	rust.SendChatMessage(player,self.Config.Settings.messageName,self.Config.Messages.NoteCMDAdmin1)
    	rust.SendChatMessage(player,self.Config.Settings.messageName,self.Config.Messages.NoteCMDAdmin2)
    end
end