﻿using RevokeMsgPatcher.Model;
using RevokeMsgPatcher.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace RevokeMsgPatcher.Matcher
{
    public class ModifyFinder
    {
        // TODO 该逻辑需要优化！
        public static List<Change> FindChanges(string path, List<ReplacePattern> replacePatterns)
        {
            // 读取整个文件(dll) // 20200721 钉钉:读取 先不改了！
            byte[] fileByteArray = FileUtil.ReadFileBytes(path, replacePatterns[0].Start);

            List<Change> changes = new List<Change>(); // 匹配且需要替换的地方

            // 查找所有替换点
            int matchNum = 0; // 匹配数量
            foreach (ReplacePattern pattern in replacePatterns)
            {
                // 所有的匹配点位
                int[] matchIndexs = FuzzyMatcher.MatchAll(fileByteArray, pattern.Search);
                if (matchIndexs.Length == 1)
                {
                    matchNum++;
                    // 与要替换的串不一样才需要替换（当前的特征肯定不一样）
                    if (!FuzzyMatcher.IsEqual(fileByteArray, matchIndexs[0], pattern.Replace))
                    {
                        changes.Add(new Change(matchIndexs[0], pattern.Replace));
                    }
                }
            }

            // 匹配数和期望的匹配数不一致时报错（当前一个特征只会出现一次）
            if (matchNum != replacePatterns.Count)
            {
                Tuple<bool, SortedSet<string>> res = IsAllReplaced(fileByteArray, replacePatterns);
                if (res.Item1)
                {
                    throw new BusinessException("match_already_replace", "特征比对：当前应用已经安装了对应功能的补丁！");
                }
                else
                {
                    if (res.Item2.Count > 0)
                    {
                        throw new BusinessException("match_inconformity", $"特征比对：以下功能补丁已经安装，请取消勾选！\n已安装功能：【{string.Join("、", res.Item2)}】");
                    }
                    else
                    {
                        throw new BusinessException("match_inconformity", $"特征比对：当前特征码匹配数[{matchNum}]和期望的匹配数[{replacePatterns.Count}]不一致。\n" +
                            $"出现此种情况的一般有如下可能：\n" +
                            $"1. 你可能已经安装了某个功能的补丁，请选择未安装功能进行安装。\n" +
                            $"2. 如果当前版本为最新版本，特征码可能出现变化（可能性比较低），请联系作者处理。");
                    }
                }
            }
            else
            {
                // 匹配数和需要替换的数量不一致时，可能时部分/所有特征已经被替换
                if (matchNum != changes.Count)
                {
                    // 此逻辑在当前特征配置下不会进入，因为查找串和替换串当前全部都是不相同的
                    if (changes.Count == 0)
                    {
                        throw new BusinessException("match_already_replace", "特征比对：当前应用已经安装了所选功能补丁！");
                    }
                    else
                    {
                        throw new BusinessException("match_part_replace", "特征比对：部分特征已经被替换，请确认是否有使用过其他防撤回/多开补丁！");
                    }

                }
                else
                {
                    // 匹配数和需要替换的数量一致时才是正常状态
                    return changes;
                }
            }
        }

        private static Tuple<bool, SortedSet<string>> IsAllReplaced(byte[] fileByteArray, List<ReplacePattern> replacePatterns)
        {
            int matchNum = 0;
            SortedSet<string> alreadyReplaced = new SortedSet<string>(); // 已经被替换特征的功能
            foreach (ReplacePattern pattern in replacePatterns)
            {
                // 所有的匹配点位
                int[] matchIndexs = FuzzyMatcher.MatchAll(fileByteArray, pattern.Replace);
                if (matchIndexs.Length == 1)
                {
                    matchNum++;
                    alreadyReplaced.Add(pattern.Category);
                }
            }
            return new Tuple<bool, SortedSet<string>>(matchNum == replacePatterns.Count, alreadyReplaced);
        }
    }
}
