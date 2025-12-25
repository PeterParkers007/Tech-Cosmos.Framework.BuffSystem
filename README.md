# GBF - Buff System Framework

## 🎮 核心特性

### 1. **完整的生命周期管理**
- 统一的创建 → 应用 → 更新 → 移除流程
- 自动处理效果应用和回滚
- 避免状态不一致和竞态条件

### 2. **灵活的执行控制**
- **时间暂停**：只暂停计时器
- **逻辑暂停**：完全停止Buff更新
- **时间缩放**：加速/减速Buff效果

### 3. **安全的状态流转**
- 所有移除操作通过统一通道处理
- 避免双重回滚等数值错误
- 一帧延迟设计确保逻辑安全

### 4. **强大的查询系统**
- 支持Any/All两种标签匹配模式
- 按标签查询、移除Buff
- 批量操作支持

## 📁 架构概览

### 核心组件

```
BuffSystem (管理层)
    ↓ 管理
IBuff (接口层)
    ↓ 实现
BaseBuff (基类层)
    ↓ 包含
BuffEffectExecuter (执行层)
    ↓ 组合
    ├── BuffEffect (效果层)
    └── ExecutionMode (执行模式层)
```

## 🚀 快速开始

### 1. 在单位类中集成BuffSystem

```csharp
public class Player : MonoBehaviour
{
    // 每个单位持有一个BuffSystem管理自己的所有buff/debuff
    private BuffSystem<Player> _buffSystem;
    
    void Awake()
    {
        _buffSystem = new BuffSystem<Player>(this);
        
        // 可选：订阅Buff事件
        _buffSystem.OnBuffAdded += OnBuffAdded;
        _buffSystem.OnBuffRemoved += OnBuffRemoved;
    }
    
    void Update()
    {
        // 每帧更新自己的Buff系统
        _buffSystem.BuffUpdate(Time.deltaTime);
    }
    
    public void TakeBuff(IBuff<Player> buff)
    {
        _buffSystem.AddBuff(buff);
    }
    
    public bool HasBuff(params string[] tags) => _buffSystem.HasAnyBuff(tags);
    
    private void OnBuffAdded(IBuff<Player> buff)
    {
        Debug.Log($"玩家获得Buff: {buff.GetType().Name}");
    }
    
    private void OnBuffRemoved(IBuff<Player> buff)
    {
        Debug.Log($"玩家失去Buff: {buff.GetType().Name}");
    }
}
```

### 2. 在外部施加Buff

```csharp
public class SkillManager : MonoBehaviour
{
    public void CastPoisonSpell(Player targetPlayer)
    {
        // 创建中毒Buff
        PoisonBuff poison = new PoisonBuff(targetPlayer, 10.0f);
        
        // 施加给目标玩家（玩家自己的BuffSystem会管理它）
        targetPlayer.TakeBuff(poison);
    }
    
    public void CurePoison(Player targetPlayer)
    {
        // 移除玩家的所有中毒相关Buff
        targetPlayer.GetBuffSystem().RemoveBuffsByAnyTag("poison", "dot");
    }
}
```

### 3. Buff效果执行器示例

```csharp
// 周期伤害效果（每2秒执行一次）
public class PoisonDamageExecuter : BuffEffectExecuter<Player>
{
    public PoisonDamageExecuter() 
        : base(new PeriodicityMode<Player>(2.0f), new PoisonDamageEffect())
    {
    }
}

// 伤害效果实现
public class PoisonDamageEffect : BuffEffect<Player>
{
    public override void Effect(Player target)
    {
        target.TakeDamage(15);
        Debug.Log($"中毒伤害: 对 {target.name} 造成15点伤害");
    }
}

// 一次性效果（只在Buff应用时执行一次）
public class InstantHealExecuter : BuffEffectExecuter<Player>
{
    public InstantHealExecuter()
        : base(new ContinuityMode<Player>(), new HealEffect())
    {
    }
}

public class HealEffect : BuffEffect<Player>
{
    public override void Effect(Player target)
    {
        target.Heal(50);
        Debug.Log($"立即治疗: {target.name} 恢复50点生命");
    }
}
```

### 4. 自定义Buff类

```csharp
public class PoisonBuff : BaseBuff<Player>
{
    public PoisonBuff(Player target, float duration) 
        : base(target, duration, new string[] { "damage", "dot", "poison", "debuff" })
    {
        priority = 5; // 中等优先级
        
        // 添加效果执行器
        AddEffectExecuter(new PoisonDamageExecuter());
        
        // 可选：添加视觉特效
        OnApply += (player) => player.ShowPoisonVFX();
        OnRemove += (player) => player.HidePoisonVFX();
    }
}

public class SpeedBoostBuff : BaseBuff<Player>
{
    private float originalSpeed;
    
    public SpeedBoostBuff(Player target, float duration, float multiplier)
        : base(target, duration, new string[] { "movement", "speed", "buff" })
    {
        priority = 3;
        
        // 使用可回滚的效果
        AddEffectExecuter(new SpeedBoostExecuter(multiplier));
    }
}

// 可回滚的速度提升效果
public class SpeedBoostExecuter : BuffEffectExecuter<Player>, IRollBack
{
    private float multiplier;
    private float originalSpeed;
    
    public SpeedBoostExecuter(float multiplier) 
        : base(new ContinuityMode<Player>(), new SpeedEffect())
    {
        this.multiplier = multiplier;
    }
    
    public override void Apply(Player target)
    {
        // 记录原始速度并应用加成
        originalSpeed = target.MoveSpeed;
        target.MoveSpeed *= multiplier;
        base.Apply(target);
    }
    
    public void RollBack()
    {
        // Buff结束时恢复原始速度
        if (base.target is Player player)
        {
            player.MoveSpeed = originalSpeed;
        }
    }
    
    private class SpeedEffect : BuffEffect<Player>
    {
        public override void Effect(Player target)
        {
            // 可以在这里触发速度提升的特效等
            Debug.Log($"速度提升生效");
        }
    }
}
```

### 5. 完整使用示例

```csharp
public class GameSceneExample : MonoBehaviour
{
    public Player player;
    public Enemy enemy;
    
    void Start()
    {
        // 玩家给自己加增益Buff
        SpeedBoostBuff speedBuff = new SpeedBoostBuff(player, 8.0f, 1.5f);
        player.TakeBuff(speedBuff);
        
        // 敌人给玩家施加减益Buff
        PoisonBuff poison = new PoisonBuff(player, 12.0f);
        player.TakeBuff(poison);
    }
    
    void Update()
    {
        // 检查玩家状态
        if (player.HasBuff("poison"))
        {
            Debug.Log("玩家处于中毒状态");
        }
        
        if (player.HasBuff("speed", "buff"))
        {
            Debug.Log("玩家有速度加成");
        }
        
        // 使用道具清除所有减益
        if (Input.GetKeyDown(KeyCode.C))
        {
            var buffSystem = player.GetBuffSystem();
            buffSystem.RemoveBuffsByAnyTag("debuff", "dot");
        }
    }
}
```

## 🔍 API参考

### BuffSystem<T> 主要方法

```csharp
// 添加Buff
void AddBuff(IBuff<T> buff);
void AddBuff(params IBuff<T>[] buffs);

// 移除Buff
void ManualRemoveBuff(IBuff<T> buff);  // 安全移除（标记后下一帧移除）
void RemoveBuff(IBuff<T> buff);        // 立即移除（小心使用）

// 按标签移除
void RemoveBuffsByAnyTag(params string[] tags);    // 包含任意标签
void RemoveBuffsByAllTags(params string[] tags);   // 包含所有标签

// 查询Buff
bool HasAnyBuff(params string[] tags);             // 是否存在任意匹配
bool HasAllBuff(params string[] tags);             // 是否存在全部匹配
IBuff<T> FindBuffByAnyTag(params string[] tags);   // 查找第一个匹配
List<IBuff<T>> FindAllBuffsByAnyTag(params string[] tags); // 查找所有匹配

// 排序
void SortBuffs();  // 按优先级排序
```

### BaseBuff<T> 主要功能

```csharp
// 时间控制
void Pause();              // 逻辑暂停
void TimePause();          // 时间暂停
void SetTimeScale(float scale); // 时间缩放

// 持续时间管理
void ExtendDuration(float extraTime);     // 延长时间
void SetRemainingTime(float remaining);   // 设置剩余时间
void Refresh();                           // 刷新计时器

// 状态查询
float RemainingTime { get; }  // 剩余时间
float Progress { get; }       // 进度(0-1)
bool IsPaused { get; }        // 是否暂停

// 事件
event Action<T> OnApply;      // 应用时触发
event Action<T> OnRemove;     // 移除时触发
```

### 执行模式 (ExecutionMode)

```csharp
// 连续模式 - 只在第一次执行
ContinuityMode<T> mode = new ContinuityMode<T>();

// 周期模式 - 按固定间隔执行
PeriodicityMode<T> mode = new PeriodicityMode<T>(interval: 2.0f);

// 自定义模式 - 继承ExecutionMode<T>
public class CustomMode<T> : ExecutionMode<T> where T : class
{
    public override bool IsEligible()
    {
        // 自定义执行条件
        return true;
    }
    
    public override void Execution(Action<T> applyAction)
    {
        // 自定义执行逻辑
        applyAction?.Invoke(target);
    }
}
```

## 💡 最佳实践

### 1. **标签使用规范**
```csharp
// 使用有意义的标签组合
new string[] { "element.fire", "damage.over_time", "debuff" }

// 标签分组建议
- element.*       // 元素类型
- damage.*        // 伤害类型  
- buff/debuff     // 增益/减益
- status.*        // 状态类型
```

### 2. **优先级设计**
```csharp
// 建议的优先级范围
public static class BuffPriority
{
    public const int Critical = 100;    // 关键Buff（如无敌）
    public const int High = 50;         // 重要Buff（如攻击提升）
    public const int Normal = 0;        // 普通Buff
    public const int Low = -50;         // 次要Buff
}
```

### 3. **效果设计模式**
```csharp
// 可回滚的效果
public class StatModifierEffect : BuffEffect<Player>, IRollBack
{
    private float originalValue;
    
    public override void Effect(Player target)
    {
        originalValue = target.AttackPower;
        target.AttackPower *= 1.5f;  // 增加50%攻击力
    }
    
    public void RollBack()
    {
        target.AttackPower = originalValue;  // 恢复原值
    }
}
```

### 4. **性能优化建议**
```csharp
// 频繁查询时缓存标签集合
public class OptimizedBuff : BaseBuff<Player>
{
    private HashSet<string> _tagSet;
    
    public new string[] tags 
    { 
        set 
        { 
            base.tags = value;
            _tagSet = new HashSet<string>(value);
        }
    }
    
    public bool HasTag(string tag) => _tagSet.Contains(tag);
}
```

## ⚠️ 注意事项

### 1. **移除操作安全**
- 使用 `ManualRemoveBuff()` 进行安全移除
- 避免直接调用 `buff.Remove()` 可能导致状态不一致
- 移除操作会有1帧延迟，这是设计特性

### 2. **执行模式选择**
- **ContinuityMode**: 适用于一次性效果（如立即治疗）
- **PeriodicityMode**: 适用于持续效果（如中毒伤害）
- 自定义模式: 复杂条件触发（如血量低于30%时）

### 3. **事件处理**
- OnApply/OnRemove 事件在Buff内部触发
- OnBuffAdded/OnBuffRemoved 事件在系统层触发
- 注意事件触发时机，避免循环调用

### 4. **Unity集成**
```csharp
// 在MonoBehaviour中管理
public class PlayerBuffManager : MonoBehaviour
{
    private BuffSystem<Player> buffSystem;
    
    void Awake()
    {
        buffSystem = new BuffSystem<Player>(GetComponent<Player>());
    }
    
    void Update()
    {
        buffSystem.BuffUpdate(Time.deltaTime);
    }
}
```

## 🔄 扩展指南

### 1. **添加新的执行模式**
```csharp
public class HealthThresholdMode<T> : ExecutionMode<T> where T : class
{
    private float threshold;
    
    public HealthThresholdMode(float healthThreshold)
    {
        threshold = healthThreshold;
    }
    
    public override bool IsEligible()
    {
        // 仅当目标血量低于阈值时执行
        if (target is IDamageable damageable)
            return damageable.HealthPercentage < threshold;
        return false;
    }
    
    public override void Execution(Action<T> applyAction)
    {
        applyAction?.Invoke(target);
    }
}
```

### 2. **创建复合Buff**
```csharp
public class FirePoisonComboBuff : BaseBuff<Player>
{
    public FirePoisonComboBuff(Player target, float duration)
        : base(target, duration, new string[] { "fire", "poison", "combo" })
    {
        // 添加多个效果
        AddEffectExecuter(new FireDamageEffect());
        AddEffectExecuter(new PoisonDamageEffect());
        AddEffectExecuter(new MovementSlowEffect());
    }
}
```

### 3. **Buff叠加系统**
```csharp
public class StackableBuffSystem<T> : BuffSystem<T> where T : class
{
    private Dictionary<string, List<IBuff<T>>> buffStacks = new();
    
    public void AddStackableBuff(IBuff<T> buff, int maxStacks = 3)
    {
        string key = GetBuffKey(buff);
        
        if (!buffStacks.ContainsKey(key))
            buffStacks[key] = new List<IBuff<T>>();
            
        var stack = buffStacks[key];
        if (stack.Count >= maxStacks)
        {
            // 移除最早的效果
            ManualRemoveBuff(stack[0]);
            stack.RemoveAt(0);
        }
        
        stack.Add(buff);
        AddBuff(buff);
    }
}
```

## 📝 版本记录

### v1.0.0 (当前)
- 基础Buff系统框架
- 完整的生命周期管理
- 时间控制与执行模式
- 标签查询系统
- 安全的状态管理

### 未来可能的计划
- [ ] Buff可视化调试工具
- [ ] 网络同步支持
- [ ] Buff配置数据驱动
- [ ] 效果组合编辑器

## 🤝 贡献

这是一个实战驱动的框架，欢迎提出：
- 实际使用中发现的问题
- 游戏开发中的特殊需求
- 性能优化建议
- 更好的API设计

## 许可证

MIT License - 详见 [LICENSE](LICENSE) 文件