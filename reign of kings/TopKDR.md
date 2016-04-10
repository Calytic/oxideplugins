This plugin displays the kill and death ratio with top list in-game.

**Commands:**

/top [Number] || ex. /top 5, /top 10 => Displays the set amount of the top KD ratios.

**Configuration File(oxide/config):**

````

{

  "ScoreTags": {

    "Tags": {

      "[Tag1]": 5,

      "(Tag2)": 10,

      "[Tag3]": 15,

      "{Tag4}": 20,

      "$Tag5$": 25

    }

  },

  "Settings": {

    "EnableScoreTags": false

  }

}

 
````


**Lang File(oxide/lang/TopKDR.en.json):**

````

{

  "TopList": "Name: {0}, Kills: {1}, Deaths: {2}, Score: {3}"

}


 
````