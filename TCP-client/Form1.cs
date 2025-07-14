using System;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using ClassLibrary_Message;
using System.Threading;
using System.Runtime.Serialization.Json;

namespace TCP_client
{
    public partial class Form1 : Form
    {
        TcpClient tcpClient;
        NetworkStream netstream;
        public SynchronizationContext uiContext;

        public Form1()
        {
            InitializeComponent();
            // Получим контекст синхронизации для текущего потока 
            uiContext = SynchronizationContext.Current;
        }

        private async void Connect()
        {
            await Task.Run( async() =>
            {
                // соединяемся с удаленным устройством
                try
                {
                    string IP = null;
                    uiContext.Send(d => IP = ip_address.Text, null);
                    // Конструктор TcpClient инициализирует новый экземпляр класса и подключает его к указанному порту заданного узла.
                    tcpClient = new TcpClient(IP /* IP-адрес хоста */, 49152 /* порт */);
                    // Получим объект NetworkStream, используемый для приема и передачи данных.
                    netstream = tcpClient.GetStream();
                    byte[] msg = Encoding.Default.GetBytes(Dns.GetHostName() /* имя узла локального компьютера */);// конвертируем строку, содержащую имя хоста, в массив байтов
                    await netstream.WriteAsync(msg, 0, msg.Length); // записываем данные в NetworkStream.
                    MessageBox.Show("Клиент " + Dns.GetHostName() + " установил соединение с " + IP);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Клиент: " + ex.Message);
                }
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Connect();
        }

        private async void Exchange()
        {
            await Task.Run(async() =>
            {
                try
                {                  
                    MessageTCP m = new MessageTCP();
                    string message = null;
                    uiContext.Send(d => message = textBox1.Text, null);
                    m.Message = message; // текст сообщения
                    m.Host = Dns.GetHostName(); // имя хоста
                    m.User = Environment.UserDomainName + @"\" + Environment.UserName; // имя пользователя

                    // Создадим поток, резервным хранилищем которого является память.
                    MemoryStream stream = new MemoryStream();
                    var jsonFormatter = new DataContractJsonSerializer(typeof(MessageTCP));
                    jsonFormatter.WriteObject(stream, m);
                    byte[] arr = stream.ToArray(); // записываем содержимое потока в байтовый массив
                    stream.Close();

                    await netstream.WriteAsync(arr, 0, arr.Length); // записываем данные в NetworkStream.
                    if (m.Message.IndexOf("<end>") > -1) // если клиент отправил эту команду, то принимаем сообщение от сервера
                    {
                        arr = new byte[tcpClient.ReceiveBufferSize];
                        // Читаем данные из объекта NetworkStream.
                        int len = await netstream.ReadAsync(arr, 0, tcpClient.ReceiveBufferSize);// Возвращает фактически считанное число байтов
                        MessageBox.Show("Сервер ответил: " + Encoding.Default.GetString(arr, 0, len) /*конвертируем массив байтов в строку*/);
                        netstream.Close();
                        tcpClient.Close(); // закрываем TCP-подключение и освобождаем все ресурсы, связанные с объектом TcpClient.
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Клиент: " + ex.Message);
                }
            });
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Exchange();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                netstream?.Close();
                tcpClient?.Close(); // закрываем TCP-подключение и освобождаем все ресурсы, связанные с объектом TcpClient.
            }
            catch (Exception ex)
            {
                MessageBox.Show("Клиент: " + ex.Message);
            }
        }
    }
}
