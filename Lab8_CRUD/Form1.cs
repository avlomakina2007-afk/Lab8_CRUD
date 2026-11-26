using System;
using System.Data;
using System.Data.SqlClient;   
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Lab8_CRUD
{
    public partial class Form1 : Form
    {
        private string connectionString =
            @"Data Source=LOMAKINA22;Initial Catalog=StudentsDB;Integrated Security=True";

        private int selectedStudentId = -1;   // Id выбранного студента

        public Form1()
        {
            InitializeComponent();

            // Привязка обработчиков
            this.Load += Form1_Load;
            dataGridViewStudents.SelectionChanged += dataGridViewStudents_SelectionChanged;
            buttonAdd.Click += buttonAdd_Click;
            buttonUpdate.Click += buttonUpdate_Click;
            buttonDelete.Click += buttonDelete_Click;
            buttonClear.Click += buttonClear_Click;
            buttonExport.Click += buttonExport_Click;   
            buttonUndo.Click += buttonUndo_Click;     
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            LoadStudents();
        }

        private void LoadStudents(string searchText = "")
        {
            try
            {
                // Базовый запрос
                string sql = "SELECT Id, FullName, Age, GroupName, AverageGrade FROM Students";

                // Если в поиске есть текст, добавляем условие WHERE
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    sql += " WHERE FullName LIKE @search";
                }

                sql += " ORDER BY Id";

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter(sql, conn))
                {
                    // Если поиск активен, передаем параметр
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        // Знаки % означают "любые символы до и после"
                        adapter.SelectCommand.Parameters.AddWithValue("@search", "%" + searchText.Trim() + "%");
                    }

                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dataGridViewStudents.DataSource = dt;

                    // ... (остальной код настройки колонок из вашей 8-й лабы оставляем без изменений) ...
                    if (dataGridViewStudents.Columns.Contains("Id"))
                        dataGridViewStudents.Columns["Id"].Visible = false;
                }
                UpdateStatus("Данные загружены");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        //  CREATE  (с проверкой дубликата)
        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string checkSql = "SELECT COUNT(*) FROM Students WHERE FullName = @name";
                    using (SqlCommand checkCmd = new SqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@name", textBoxName.Text.Trim());
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show("Студент с таким ФИО уже существует!",
                                "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    string sql = @"INSERT INTO Students (FullName, Age, GroupName, AverageGrade)
                                   VALUES (@name, @age, @group, @grade)";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", textBoxName.Text.Trim());
                        cmd.Parameters.AddWithValue("@age", (int)numericAge.Value);
                        cmd.Parameters.AddWithValue("@group", textBoxGroup.Text.Trim());
                        cmd.Parameters.AddWithValue("@grade", numericGrade.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadStudents();
                ClearFields();
                UpdateStatus("Студент добавлен");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //  UPDATE

        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            if (selectedStudentId < 0)
            {
                MessageBox.Show("Выберите студента для редактирования!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!ValidateInput()) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"UPDATE Students
                                   SET FullName = @name,
                                       Age = @age,
                                       GroupName = @group,
                                       AverageGrade = @grade
                                   WHERE Id = @id";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", textBoxName.Text.Trim());
                        cmd.Parameters.AddWithValue("@age", (int)numericAge.Value);
                        cmd.Parameters.AddWithValue("@group", textBoxGroup.Text.Trim());
                        cmd.Parameters.AddWithValue("@grade", numericGrade.Value);
                        cmd.Parameters.AddWithValue("@id", selectedStudentId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadStudents();
                ClearFields();
                UpdateStatus("Данные обновлены");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //  DELETE  (с подтверждением)

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (selectedStudentId < 0)
            {
                MessageBox.Show("Выберите студента для удаления!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Удалить студента \"{textBoxName.Text}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "DELETE FROM Students WHERE Id = @id";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedStudentId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadStudents();
                ClearFields();
                UpdateStatus("Студент удалён");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //  CLEAR
        private void buttonClear_Click(object sender, EventArgs e)
        {
            ClearFields();
            UpdateStatus("Поля очищены");
        }

        private void ClearFields()
        {
            textBoxName.Clear();
            numericAge.Value = numericAge.Minimum;
            textBoxGroup.Clear();
            numericGrade.Value = 0;
            selectedStudentId = -1;
            dataGridViewStudents.ClearSelection();
        }

        //  SELECTION CHANGED — подстановка данных в поля

        private void dataGridViewStudents_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridViewStudents.CurrentRow == null) return;
            var row = dataGridViewStudents.CurrentRow;
            if (row.Cells["Id"].Value == null) return;

            selectedStudentId = Convert.ToInt32(row.Cells["Id"].Value);
            textBoxName.Text = row.Cells["FullName"].Value?.ToString() ?? "";

            numericAge.Value = row.Cells["Age"].Value != null
                ? Convert.ToDecimal(row.Cells["Age"].Value) : numericAge.Minimum;

            textBoxGroup.Text = row.Cells["GroupName"].Value?.ToString() ?? "";

            numericGrade.Value = row.Cells["AverageGrade"].Value != null
                ? Convert.ToDecimal(row.Cells["AverageGrade"].Value) : 0;
        }

        //  ВАЛИДАЦИЯ
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(textBoxName.Text))
            {
                MessageBox.Show("Введите ФИО!", "Валидация",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxName.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(textBoxGroup.Text))
            {
                MessageBox.Show("Введите группу!", "Валидация",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxGroup.Focus();
                return false;
            }
            return true;
        }

        //  ЭКСПОРТ В CSV

        private void buttonExport_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV файл (*.csv)|*.csv";
                sfd.FileName = "students.csv";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Id;ФИО;Возраст;Группа;Средний балл");

                    foreach (DataGridViewRow row in dataGridViewStudents.Rows)
                    {
                        if (row.IsNewRow) continue;
                        sb.AppendLine(
                            $"{row.Cells["Id"].Value};" +
                            $"{row.Cells["FullName"].Value};" +
                            $"{row.Cells["Age"].Value};" +
                            $"{row.Cells["GroupName"].Value};" +
                            $"{row.Cells["AverageGrade"].Value}");
                    }

                    // BOM для корректного отображения кириллицы в Excel
                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);

                    MessageBox.Show("Экспорт завершён!", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка экспорта: " + ex.Message, "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //  ОТМЕНА последнего изменения
        private void buttonUndo_Click(object sender, EventArgs e)
        {
            LoadStudents();
            ClearFields();
            UpdateStatus("Изменения отменены (таблица перезагружена)");
        }

        //  СТАТУС-БАР со счётчиком студентов 

        private void UpdateStatus(string message)
        {
            int count = 0;
            foreach (DataGridViewRow row in dataGridViewStudents.Rows)
                if (!row.IsNewRow) count++;

            labelStatus.Text = $"{message} | Всего студентов: {count}";
        }

        // Нажатие на кнопку "Найти"
        private void buttonSearch_Click(object sender, EventArgs e)
        {
            // Вызываем загрузку с текстом из поисковой строки
            LoadStudents(textBoxSearch.Text);
        }

    }
}