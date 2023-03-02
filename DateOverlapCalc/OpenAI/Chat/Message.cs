namespace DateOverlapCalc.OpenAI.Chat
{
    class Message
    {
        public string role;
        public string content;

        public Message(string role, string content)
        {
            this.role = role;
            this.content = content;
        }

        public void Add(string message)
        {
            // Add to an array of history
        }
    }
}
