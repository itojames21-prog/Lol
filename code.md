# Código / Code

| Arquivo / File | Português | English |
| --- | --- | --- |
| `SpaceDistortion.cs` | Ponto de entrada do mod, registra todos os hooks. | Mod entry point, registers all hooks. |
| `SpaceDistortionBoss.cs` | IA completa do chefe com 10 estados, transições de fase, animação e renderização customizada. | Complete boss AI with 10 states, phase transitions, animation, and custom rendering. |
| `SpaceDistortionProjectiles.cs` | 4 tipos de projéteis customizados com IA única, efeitos visuais e renderização. | 4 custom projectile types with unique AI, visual effects, and rendering. |
| `SpaceDistortionSpawnItem.cs` | Item de invocação (Chave da Fenda Cósmica) com verificação de noite e spawn. | Summon item (Cosmic Rift Key) with night check and spawn logic. |
| `SpaceDistortionRecipe.cs` | Sistema de receitas para craftar o item de invocação. | Recipe system for crafting the summon item. |

## Arquitetura / Architecture

```
SpaceDistortion (Mod)
├── Load() → Registra hooks NPC, Projectile, Item, WorldGen
├── Unload() → Limpa referências estáticas
└── WorldGen_UpdateWorld() → Hook para condições de spawn natural
```

### SpaceDistortionBoss (NPC Type: 6000)

**Estados de IA / AI States:**
| Estado | Valor | Descrição |
|--------|-------|-----------|
| Spawn | 0 | Entrada dramática do topo da tela |
| Phase1_Idle | 1 | Pairando, decidindo próximo ataque |
| Phase1_Charge | 2 | Investida telegrafada no jogador |
| Phase1_VoidBoltBarrage | 3 | Orbita disparando projéteis teleguiados |
| Phase1_DistortionWave | 4 | Anéis expansivos de puxão gravitacional |
| Phase2_Transition | 5 | Transformação dramática a 50% HP |
| Phase2_BlackHole | 6 | Invoca buracos negros + fragmentos estelares |
| Phase2_StarRain | 7 | Chuva de projéteis do alto |
| Phase2_VoidCharge | 8 | Carga aprimorada deixando buracos negros |
| Enraged | 9 | Todos os ataques combinados, stats aumentados |
| Death | 10 | Animação de morte explosiva |

**Transições de Fase:**
- 50% HP → Fase 2 (cura leve para 52%, transforma visualmente)
- 10% HP → Enfurecido (dano +50%, defesa -20%)

**Renderização Customizada:**
- Sprite sheet 8 frames (120x120 cada)
- Glow pulsante baseado em fase
- Partículas de distorção orbitando
- Tint de cor por fase (roxo → rosa → vermelho)

### SpaceDistortionProjectiles

| Projétil | ID | Comportamento | Renderização |
|----------|-----|---------------|--------------|
| VoidBolt | 6001 | Inicial reto, depois teleguia | Bolt alongado com trail |
| DistortionWave | 6002 | Anel expansivo puxa jogadores | 3 anéis concêntricos rotativos |
| StarShard | 6003 | Queda com gravidade, gira | Estrela 4-pontas dourada |
| BlackHole | 6004 | Puxa jogadores/projéteis, dano de contato | Disco de acreção + horizonte de eventos |

### SpaceDistortionSpawnItem (Item Type: 5000)

- **Nome**: Chave da Fenda Cósmica / Cosmic Rift Key
- **Uso**: Segurar (HoldUp), 45 ticks, consumível
- **Condição**: Apenas à noite (`!Main.nightTime` bloqueia)
- **Verificação**: Impede spawn duplicado
- **Efeitos**: Som de roar, partículas de vórtice, mensagem de status

### Receita / Recipe

```
18× Fragmento de Vórtice
10× Barra de Luminita
5× Alma do Poder
5× Alma da Visão
5× Alma do Medo
[Estação de Criação Antiga]
→ Chave da Fenda Cósmica
```

## Técnicas Utilizadas / Techniques Used

1. **GameHandle** - Mantém itens auxiliares fora do inventário (padrão Thunder Blade)
2. **Hooks On_*** - Intercepta SetDefaults, AI, Draw, Kill, etc.
3. **NetMessage.SendData** - Sincronização multiplayer de spawn de NPC
4. **NPC.NewNPC / Projectile.NewProjectile** - Spawn server-side com source tracking
5. **SpriteBatch.Draw customizado** - Renderização procedural sem assets externos
6. **Dust.NewDustPerfect** - Partículas otimizadas
7. **AI State Machine** - Máquina de estados limpa com enum
8. **Phase Transitions** - Verificação contínua de HP ratio
9. **LocalAI para dados visuais** - Rotação, escala, intensidade de glow
10. **SafeNormalize** - Evita NaN em vetores zero

## Referências / References

- [Bunny Loader C# Docs](https://github.com/JonataOliveiraa/Bunny-Loader/tree/main/docs/mods-em-csharp)
- ExampleModBunny
- Thunder Blade example (C# mod reference)
- Terraria ModLoader documentation