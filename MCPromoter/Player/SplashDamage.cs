using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using CSR;

namespace MCPromoter
{
    partial class MCPromoter
    {
        #region https: //github.com/zhkj-liuxiaohua/MGPlugins
        
        static Hashtable swords = new Hashtable()
        {
            {"wooden_sword",true},
            {"stone_sword",true},
            {"iron_sword",true},
            {"diamond_sword",true},
            {"golden_sword",true},
            {"netherite_sword",true}
        };
        static JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();

        public static bool AttackPlugin(Events x)
        {
            if (config.PluginDisable.Futures.SplashDamage) return true;

                var e = BaseEvent.getFrom(x) as AttackEvent;
                if (e == null) return true;
                
                if (!e.isstand) return true;
                CsPlayer csPlayer = new CsPlayer(_mapi, e.playerPtr);
                CsActor csActor = new CsActor(_mapi, e.attackedentityPtr);
                var hand = javaScriptSerializer.Deserialize<ArrayList>(csPlayer.HandContainer);
                if (hand != null && hand.Count > 0)
                {
                    var mainHand = hand[0] as Dictionary<string, object>;
                    if (mainHand != null)
                    {
                        object oid;
                        if (mainHand.TryGetValue("rawnameid", out oid))
                        {
                            string rid = oid as string;
                            var oisSword = swords[rid];
                            if (oisSword != null && (bool) oisSword)
                            {
                                //开始执行溅射伤害操作
                                var pdata = csActor.Position;
                                var aXYZ = javaScriptSerializer.Deserialize<Vec3>(csActor.Position);
                                var list = CsActor.getsFromAABB(_mapi, csActor.DimensionId, aXYZ.x - 2, aXYZ.y - 1,
                                    aXYZ.z - 2,
                                    aXYZ.x + 2, aXYZ.y + 1, aXYZ.z + 2);
                                if (list != null && list.Count > 0)
                                {
                                    int count = 0;
                                    foreach (IntPtr aptr in list)
                                    {
                                        if (aptr != e.attackedentityPtr)
                                        {
                                            CsActor spa = new CsActor(_mapi, aptr);
                                            if (((spa.TypeId & 0x100) == 0x100))
                                            {
                                                spa.hurt(e.playerPtr, ActorDamageCause.EntityAttack, 1, true, false);
                                                ++count;
                                            }
                                        }

                                        if (count >= config.MaxDamageSplash)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return true;
        }
        
        #endregion
    }
}