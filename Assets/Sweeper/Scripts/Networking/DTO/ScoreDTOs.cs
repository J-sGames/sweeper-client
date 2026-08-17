using System;
using UnityEngine;

namespace SweeperClient.DTOs
{
    [Serializable]
    public class ScoreRequest
    {
        public string Name;
        public int Score;
        public string StartedTime;
        public string EndedTime;
    }

    [Serializable]
    public class ScoreResponse
    {
        public string Name;
        public int Score;
    }
}
