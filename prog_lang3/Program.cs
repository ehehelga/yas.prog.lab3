using System.Threading.Channels;

namespace prog_lang3
{
    struct Token
    {
        public string Data;
        public int Recipient;
        public int ttl;
    }

    class TR_Node
    {
        Channel<Token>? input;
        Channel<Token>? output;
        int node_id;

        public TR_Node(int id)
        {
            node_id = id;
        }

        public void Set_Reader(Channel<Token> reader)
        {
            input = reader;
        }

        public void Set_Writer(Channel<Token> writer)
        {
            output = writer;
        }

        public async void Wait_Token()
        {
            while (true)
            {
                if (input != null)
                {
                    await input.Reader.WaitToReadAsync();
                    Token token = await input.Reader.ReadAsync();
                    await Process(token);
                }
            }
        }

        public async Task Send_Token(Token token)
        {
            if (output != null)
            {
                await output.Writer.WriteAsync(token);
                Console.WriteLine($"Token was sent from {node_id}.");
            }
        }

        public async Task Process(Token token)
        {
            Console.WriteLine($"Token is now at {node_id}.");
            token.ttl--;

            if (token.Recipient == node_id)
                Console.WriteLine($"The reiceved message: {token.Data}.");

            else if (token.ttl > 0)
            {
                if (output != null)
                {
                    await output.Writer.WriteAsync(token);
                    Console.WriteLine($"Token was sent from {node_id}.");
                }
            }
            else Console.WriteLine("Time is over.");
        }

        internal class Program
        {
            static async Task Main(string[] args)
            {
                Console.WriteLine("Enter the number of tokens: ");
                int number = Convert.ToInt32(Console.ReadLine());

                Console.WriteLine("Enter the recipient's number: ");
                int res = Convert.ToInt32(Console.ReadLine());
                if (res >= number || res == 0)
                {
                    Console.WriteLine("Error: recipient's number must be < n or > 0.");
                    res = Convert.ToInt32(Console.ReadLine());
                }

                Console.WriteLine("Enter your message: ");
                string data = Console.ReadLine();

                Console.WriteLine("Enter the token's time of life: ");
                int time = Convert.ToInt32(Console.ReadLine());

                Token token = new Token
                {
                    Data = data,
                    Recipient = res,
                    ttl = time,
                };

                TR_Node[] nodes = new TR_Node[number];
                nodes[0] = new TR_Node(0);

                for (int i = 0; i < number - 1; i++)
                {
                    Channel<Token> newChannel = Channel.CreateBounded<Token>(1);
                    Channel<Token> channel = newChannel;

                    nodes[i + 1] = new TR_Node(i + 1);
                    nodes[i].Set_Writer(channel);
                    nodes[i + 1].Set_Reader(channel);
                }

                Channel<Token> lastChannel = Channel.CreateBounded<Token>(1);
                Channel<Token> ch = lastChannel;
                nodes[0].Set_Reader(ch);
                nodes[number - 1].Set_Writer(ch);

                for (int i = 1; i < number; i++)
                {
                    Thread thr = new Thread(nodes[i].Wait_Token);
                    thr.Start();
                }

                await nodes[0].Send_Token(token);
                while (true)
                {
                    if (nodes[0].input != null)
                    {
                        await nodes[0].input.Reader.WaitToReadAsync();
                        token = await nodes[0].input.Reader.ReadAsync();
                        await nodes[0].Process(token);
                    }
                }
            }
        }
    }
}
