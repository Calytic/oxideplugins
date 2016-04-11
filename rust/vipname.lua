PLUGIN.Name = "vipname"
PLUGIN.Title = "ViP / Admin / Other - Auto Name Changer"
PLUGIN.Version = V(1, 0, 0)
PLUGIN.Description = "This plugin will autorename any player connecting to your server found in the VIP, Admin or Other lists."
PLUGIN.Author = "TheDoc - Uprising RuST Server"
PLUGIN.HasConfig = true
PLUGIN.ResourceId = 795

----------------------------------------- LOCALS -----------------------------------------

-- ----------------------------------------------------------------
-- IsVIP
--
-- Check if the SteamID is found in the list from the config file
-- ----------------------------------------------------------------
local function IsVIP(steamID, self)
	for _, value in pairs(self.Config.Vips) do
		if steamID == value then 
			return true 
		end
	end
end

-- ----------------------------------------------------------------
-- IsAdmin
--
-- Check if the SteamID is found in the list from the config file
-- ----------------------------------------------------------------
local function IsAdmin(steamID, self)
	for _, value in pairs(self.Config.Admins) do
		--print("*** Value = " .. value .. " SteamId= " .. steamID )
		if steamID == value then 
			return true 
		end
	end
end

-- ----------------------------------------------------------------
-- IsOther1
--
-- Check if the SteamID is found in the list from the config file
-- ----------------------------------------------------------------
local function IsOther1(steamID, self)
	for _, value in pairs(self.Config.Other1) do
		--print("*** Value = " .. value .. " SteamId= " .. steamID )
		if steamID == value then 
			return true 
		end
	end
end

-- ----------------------------------------------------------------
-- IsOther2
--
-- Check if the SteamID is found in the list from the config file
-- ----------------------------------------------------------------
local function IsOther2(steamID, self)
	for _, value in pairs(self.Config.Other2) do
		--print("*** Value = " .. value .. " SteamId= " .. steamID )
		if steamID == value then 
			return true 
		end
	end
end

-- ----------------------------------------------------------------
-- IsOther3
--
-- Check if the SteamID is found in the list from the config file
-- ----------------------------------------------------------------
local function IsOther3(steamID, self)
	for _, value in pairs(self.Config.Other3) do
		--print("*** Value = " .. value .. " SteamId= " .. steamID )
		if steamID == value then 
			return true 
		end
	end
end

-- --------------------------------
-- load the default config
-- --------------------------------
function PLUGIN:Init()
    self:LoadDefaultConfig()
end

-- --------------------------------
-- load the default config
-- --------------------------------
function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.VipTag = self.Config.Settings.VipTag or " ]ViP["
	self.Config.Settings.AdminTag = self.Config.Settings.AdminTag or "(Admin) "
	self.Config.Settings.Other1Tag = self.Config.Settings.Other1Tag or " ]Other1["
	self.Config.Settings.Other2Tag = self.Config.Settings.Other2Tag or " ]Other2["
	self.Config.Settings.Other3Tag = self.Config.Settings.Other3Tag or " ]Other3["
	self.Config.Settings.AddVipTag = self.Config.Settings.AddVipTag or "true"
	self.Config.Settings.AddAdminTag = self.Config.Settings.AddAdminTag or "true"
	self.Config.Settings.AddOther1Tag = self.Config.Settings.AddOther1Tag or "false"
	self.Config.Settings.AddOther2Tag = self.Config.Settings.AddOther2Tag or "false"
	self.Config.Settings.AddOther3Tag = self.Config.Settings.AddOther3Tag or "false"
	
	self.Config.Vips = self.Config.Vips or { 
		"99999999999999991",  -- Example 1
		"99999999999999992"   -- Example 2
	}

	self.Config.Admins = self.Config.Admins or { 
		"99999999999999991",  -- Example 1
		"99999999999999992"   -- Example 2
	}
	
	self.Config.Other1 = self.Config.Other1 or {
		"99999999999999991",  -- Example 1
		"99999999999999992"   -- Example 2
	}
	
	self.Config.Other2 = self.Config.Other2 or {
		"99999999999999991",  -- Example 1
		"99999999999999992"   -- Example 2
	}
	
	self.Config.Other3 = self.Config.Other3 or {
		"99999999999999991",  -- Example 1
		"99999999999999992"   -- Example 2
	}
	
    self:SaveConfig()
end

function PLUGIN:OnPlayerConnected(packet)
    if not packet then return end
    if not packet.connection then return end

	local steamID = rust.UserIDFromConnection(packet.connection)

	if self.Config.Settings.AddOther1Tag == "true" then
		if IsOther1(steamID, self) then
			local userDispName = ( packet.connection.username .. self.Config.Settings.Other1Tag)
			packet.connection.username = userDispName
		end
	end

	if self.Config.Settings.AddOther2Tag == "true" then
		if IsOther2(steamID, self) then
			local userDispName = ( packet.connection.username .. self.Config.Settings.Other2Tag)
			packet.connection.username = userDispName
		end
	end

	if self.Config.Settings.AddOther3Tag == "true" then
		if IsOther3(steamID, self) then
			local userDispName = ( packet.connection.username .. self.Config.Settings.Other3Tag)
			packet.connection.username = userDispName
		end
	end
	
	if self.Config.Settings.AddAdminTag == "true" then
		if IsAdmin(steamID, self) then
			local userDispName = ( self.Config.Settings.AdminTag .. packet.connection.username )
			packet.connection.username = userDispName
		end
	end

	if self.Config.Settings.AddVipTag == "true" then
		if IsVIP(steamID, self) then
			local userDispName = ( packet.connection.username .. self.Config.Settings.VipTag)
			packet.connection.username = userDispName
		end
	end

end
