using System;
using UnityEngine;

namespace SweeperClient.DTOs
{
    [Serializable]
    public class ScoreRequest
    {
        public string name;
        public int score;
        public string startedTime;
        public string endedTime;
    }

    [Serializable]
    public class ScoreResponse
    {
        public long id;
        public int rank;
        public string name;
        public int score;
    }

    [Serializable]
    public sealed class RankingPageResponse
    {
        public int page;
        public int pageSize;
        public bool hasNext;
        public RankingItemResponse[] items;
    }

    [Serializable]
    public sealed class RankingItemResponse
    {
        public int rank;
        public string name;
        public int score;
        public string achievedAt;
    }
}
