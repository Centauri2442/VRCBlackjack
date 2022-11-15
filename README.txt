VRCBlackjack - CentauriCore

VRCBlackjack is a prefab that allows you to create multiple tables and play Blackjack with your friends! With 7 players per table, both manual and automatic dealing, and a full chip system, it is jam-packed with features that'll make you and your friends have fun for hours!


| IMPORTANT NOTES |

While the individual tables DO support the ability to be disabled during runtime, this has not been fully tested, and the table must be enabled on world load to allow initialization!

This table only supports U# 1.0+! Any version before 1.0 are not supported!

Currently, render textures for the dealer camera are stored in Assets/BlackjackGeneratedAssets! Attempting to move this folder will just cause it to generate a new one! This will be changed in later versions!

Unless absolutely required, handle all modifications to the table settings through the Blackjack System root gameobject script. Manually editing values in each table is not fully supported!


| DEPENDENCIES |

- VRCSDK3 2022.1.1 or newer
- Udonsharp 1.0 or newer


| INSTALLATION INSTRUCTIONS |

1) Drag and drop the "Blackjack System" prefab into the root of your hierarchy! It will automatically unpack, and we will be using this for all modifications to the tables!

2) Add however many tables you'd like! I recommend keeping it at or below 4! The more tables you have, the greater the network load!

3) Modify individual table options. In the controller, you can modify chip usage globally, or change it per table! You can also change the amount of decks, the usage of "Helpers" which show card values, and the ability to change settings during runtime!

4) Press the select button on the tables and move them to wherever in your world that you'd like! DO NOT DISABLE THEM! Having the table disabled on start can and will cause initialization issues! If you'd like to disable the table to improve performance, do it during runtime after start so that the scripts will properly initialize!

5) Success! You're good to go have fun and play Blackjack!


| BUG REPORTS |

Please report all bugs on our discord server!
- https://discord.gg/HZj7KQr


Copyright 2022 CentauriCore

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.