using Complex;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Complex
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string textFormatDir = "TextFormat";
            string jsonFormatDir = "JsonFormat";
            string binaryFormatDir = "BinaryFormat";

            if (Directory.Exists(textFormatDir))
                Directory.Delete(textFormatDir, true);
            if (Directory.Exists(jsonFormatDir))
                Directory.Delete(jsonFormatDir, true);
            if (Directory.Exists(binaryFormatDir))
                Directory.Delete(binaryFormatDir, true);

            Directory.CreateDirectory(textFormatDir);
            Directory.CreateDirectory(jsonFormatDir);
            Directory.CreateDirectory(binaryFormatDir);

            Matrix[] matricesA = new Matrix[50];
            Matrix[] matricesB = new Matrix[50];

            Random rand = new Random();
            for (int i = 0; i < 50; i++)
            {
                double[,] valuesA = new double[500, 100];
                double[,] valuesB = new double[100, 500];

                for (int j = 0; j < 500; j++)
                {
                    for (int k = 0; k < 100; k++)
                    {
                        valuesA[j, k] = rand.NextDouble();
                        valuesB[k, j] = rand.NextDouble();
                    }
                }

                matricesA[i] = new Matrix(valuesA);
                matricesB[i] = new Matrix(valuesB);
            }

            Task t1 = Task.Run(async () =>
            {
                for (int i = 0; i < 50; i++)
                {
                    string filePath = Path.Combine(textFormatDir, $"Result{i}.tsv");
                    await WriteProductToFileAsync(matricesA[i], matricesB[i], filePath);
                }
            });

            Task t2 = Task.Run(async () =>
            {
                for (int i = 0; i < 50; i++)
                {
                    string filePath = Path.Combine(jsonFormatDir, $"Result{i}.json");
                    await WriteProductToFileAsync(matricesB[i], matricesA[i], filePath);
                }
            });

            Task t3 = Task.Run(async () =>
            {
                for (int i = 0; i < 50; i++)
                {
                    string filePath = Path.Combine(binaryFormatDir, $"Result{i}.dat");
                    await WriteProductToFileBinaryAsync(matricesA[i], matricesB[i], filePath);
                }
            });

            await Task.WhenAll(t1, t2, t3);
            for (int i = 0; i < 50; i++)
            {
                string textFilePathA = Path.Combine(textFormatDir, $"Result{i}.tsv");
                string textFilePathB = Path.Combine(jsonFormatDir, $"Result{i}.json");

                Task<Matrix> readTextTaskA = ReadMatrixFromFileAsync(textFilePathA);
                Task<Matrix> readTextTaskB = ReadMatrixFromFileAsync(textFilePathB);

                Matrix matrixA = await readTextTaskA;
                Matrix matrixB = await readTextTaskB;
                bool isEqual = matrixA.Equals(matrixB);
                Console.WriteLine($"Are matrices {i} equal? {isEqual}");
            }

            Console.ReadKey();
        }
        public static async Task WriteProductToFileAsync(Matrix a, Matrix b, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                await writer.WriteLineAsync("Matrix A:");
                await writer.WriteLineAsync(a.ToString());
                await writer.WriteLineAsync("Matrix B:");
                await writer.WriteLineAsync(b.ToString());
                await writer.WriteLineAsync("Product of A and B:");
                await writer.WriteLineAsync(MatrixOperations.Multiply(a, b).ToString());
            }
        }
        public static async Task WriteProductToFileBinaryAsync(Matrix a, Matrix b, string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(a.Rows);
                writer.Write(a.Columns);
                for (int i = 0; i < a.Rows; i++)
                {
                    for (int j = 0; j < a.Columns; j++)
                    {
                        writer.Write(a[i, j]);
                    }
                }

                writer.Write(b.Rows);
                writer.Write(b.Columns);
                for (int i = 0; i < b.Rows; i++)
                {
                    for (int j = 0; j < b.Columns; j++)
                    {
                        writer.Write(b[i, j]);
                    }
                }
            }
        }
        public static async Task<Matrix> ReadMatrixFromFileAsync(string filePath)
        {
            Matrix matrix;
            using (StreamReader reader = new StreamReader(filePath))
            {
                // Read matrix size
                string[] dimensions = (await reader.ReadLineAsync()).Split(' ');
                int rows = int.Parse(dimensions[0]);
                int cols = int.Parse(dimensions[1]);

                double[,] values = new double[rows, cols];
                for (int i = 0; i < rows; i++)
                {
                    string[] line = (await reader.ReadLineAsync()).Split('\t');
                    for (int j = 0; j < cols; j++)
                    {
                        values[i, j] = double.Parse(line[j]);
                    }
                }
                matrix = new Matrix(values);
            }
            return matrix;
        }
    }
    public class Matrix
    {
        private double[,] values;

        public Matrix(double[,] values)
        {
            this.values = values;
        }

        public double this[int i, int j]
        {
            get { return values[i, j]; }
        }

        public int Rows
        {
            get { return values.GetLength(0); }
        }
        public int Columns
        {
            get { return values.GetLength(1); }
        }

        public override string ToString()
        {
            string result = "";
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    result += values[i, j].ToString() + "\t";
                }
                result += "\n";
            }
            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Matrix))
                return false;

            Matrix other = (Matrix)obj;

            if (this.Rows != other.Rows || this.Columns != other.Columns)
                return false;

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    if (this[i, j] != other[i, j])
                        return false;
                }
            }
            return true;
        }
    }
    public static class MatrixOperations
    {
        public static Matrix Transpose(Matrix matrix)
        {
            int rows = matrix.Columns;
            int cols = matrix.Rows;
            double[,] result = new double[rows, cols];
            Parallel.For(0, rows, i =>
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = matrix[j, i];
                }
            });
            return new Matrix(result);
        }

        public static Matrix Multiply(Matrix a, double scalar)
        {
            int rows = a.Rows;
            int cols = a.Columns;
            double[,] result = new double[rows, cols];
            Parallel.For(0, rows, i =>
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = a[i, j] * scalar;
                }
            });
            return new Matrix(result);
        }

        public static Matrix Add(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Columns != b.Columns)
                throw new ArgumentException("Matrices must have the same dimensions for addition.");

            int rows = a.Rows;
            int cols = a.Columns;
            double[,] result = new double[rows, cols];
            Parallel.For(0, rows, i =>
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = a[i, j] + b[i, j];
                }
            });
            return new Matrix(result);
        }

        public static Matrix Subtract(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Columns != b.Columns)
                throw new ArgumentException("Matrices must have the same dimensions for subtraction.");

            int rows = a.Rows;
            int cols = a.Columns;
            double[,] result = new double[rows, cols];
            Parallel.For(0, rows, i =>
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = a[i, j] - b[i, j];
                }
            });
            return new Matrix(result);
        }

        public static Matrix Multiply(Matrix a, Matrix b)
        {
            if (a.Columns != b.Rows)
                throw new ArgumentException("Number of columns in the first matrix must be equal to the number of rows in the second matrix for multiplication.");

            int rows = a.Rows;
            int cols = b.Columns;
            int common = a.Columns;
            double[,] result = new double[rows, cols];
            Parallel.For(0, rows, i =>
            {
                for (int j = 0; j < cols; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < common; k++)
                    {
                        sum += a[i, k] * b[k, j];
                    }
                    result[i, j] = sum;
                }
            });
            return new Matrix(result);
        }

        public static (Matrix, double) Inverse(Matrix matrix)
        {
            throw new NotImplementedException();
        }
    }
}