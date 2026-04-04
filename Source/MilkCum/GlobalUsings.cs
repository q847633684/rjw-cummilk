// 重构后 Core 子命名空间，供全项目解析 MilkCumSettings / EventHelper / Lang / Constants
global using MilkCum.Core.Settings;
global using MilkCum.Core.Utils;
global using MilkCum.Core.Constants;
global using static MilkCum.Core.Constants.Constants;
// 体液系统：乳汁 Lactation、共享 Shared（FluidPool 等）
global using MilkCum.Fluids.Lactation.Hediffs;
global using MilkCum.Fluids.Lactation.Helpers;
global using MilkCum.Fluids.Lactation.Comps;
global using MilkCum.Fluids.Lactation.Jobs;
global using MilkCum.Fluids.Lactation.Givers;
global using MilkCum.Fluids.Lactation.Data;
global using MilkCum.Fluids.Shared.Data;
global using MilkCum.Fluids.Shared.Helpers;
global using MilkCum.Core.Stats;
global using MilkCum.Fluids.Lactation.World;
// Harmony 补丁
global using MilkCum.Harmony;
global using MilkCum.Integration;
global using MilkCum.Integration.DubsBadHygiene;
