using System.Net;
using System;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace server
{
    internal class Program
    {
        static void Main(string[] args)
        {

            string path = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetFullPath("Rooms1.json"))))) + "\\BD\\";// BD init
            DataBase dataBase = DataBase.InitBD(path);// BD init
            int choice;
            const string ip = "127.0.0.1"; //Ip локальный  
            const int port = 8080; //Port любой
            var tcpEndPoint = new IPEndPoint(IPAddress.Any, port); // класс конечной точки (точка подключения), принимает Ip and Port
            var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // сокет объявляем, через него все проходит + прописываем дефолтные характеристики для TCP
            tcpSocket.Bind(tcpEndPoint); // Связываем сокет с конечной точкой (кого нужно слушать)
            tcpSocket.Listen(100); // кол-во челов, которые могут подключиться

            
            while (true)
            {
                // обработчик на прием сообщения 
                var listener = tcpSocket.Accept(); //новый сокет, который обрабатывает клиента
                var buffer = new byte[256]; // массив байтов, куда будут приниматься сообщения
                var data = new StringBuilder();
                byte[] ChoiceByte = new byte[4];
                if (BusinessLogik.TryReceive(listener, ChoiceByte))//проверяем доступ к клиенту
                {
                    
                    choice = BitConverter.ToInt32(ChoiceByte, 0);
                    Console.WriteLine(choice);
                    do
                    {

                        var size = listener.Receive(buffer); // в size записывается размерность реально полученных байт
                        data.Append(Encoding.UTF8.GetString(buffer, 0, size)); // переводим и записываем текст
                    }
                    while (listener.Available > 0);
                    var message = data.ToString();
                    Console.WriteLine(message);
                    
                    string client = DataBase.GetUserObjectString(dataBase.userobject);
                    if (choice == 1)//отправить информацию про комнату по айди
                    {
                        string roomx = DataBase.GetCurrentRoomString(dataBase,message);
                        Console.WriteLine(roomx);
                        listener.Send(Encoding.UTF8.GetBytes(roomx)); //передаем какое-либо сообщение
                    }
                    if (choice == 2)//отправить информацию про бронь по айди
                    {
                        listener.Send(Encoding.UTF8.GetBytes(DataBase.GetCurrentBookingString(dataBase, message)));
                    }
                    if (choice == 3)//отправить информацию про пользователя по айди
                    {

                    }
                    if (choice == 4)//создать бронь и отправить её номер
                    {
                        var bookingId=BusinessLogik.BookingNumberRandom(dataBase);
                        Console.WriteLine(bookingId);
                        var booking = DataBase.InitBooking(message);
                        booking.id = bookingId.ToString();
                        DataBase.AddBooking(dataBase, booking);
                        listener.Send(Encoding.UTF8.GetBytes(bookingId));
                    }
                    if (choice == 5)//создать пользователя и отправить код ошибки если возникла ошибка
                    {
                        var user = DataBase.InitUser(message);
                        var check = BusinessLogik.CheckUserCreation(dataBase, user);
                        if (check.Contains('e') || check.Contains('l') || check.Contains('n'))
                        {
                            listener.Send(Encoding.UTF8.GetBytes(check));
                        }
                        else
                        {
                            user.id = BusinessLogik.UserIdCreation(dataBase);
                            Console.WriteLine(DataBase.GetUserString(user));
                            DataBase.AddUser(dataBase, user);
                            //dataBase.SaveBD();

                        }

                    }
                    
                    

                    
                }
                listener.Shutdown(SocketShutdown.Both); // отключаем и у клиента, и у сервера
                listener.Close(); // закрываем
            }
        }
    }
}
