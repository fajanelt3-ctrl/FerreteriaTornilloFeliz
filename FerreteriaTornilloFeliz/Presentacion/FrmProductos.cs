using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using FerreteriaTornilloFeliz.Entidades;
using FerreteriaTornilloFeliz.LogicaNegocios;

namespace FerreteriaTornilloFeliz.Presentacion
{
    /// <summary>
    /// Pantalla de administración de productos (registrar, buscar, editar y desactivar).
    /// Solo maneja la interfaz: las reglas de negocio y el acceso a SQL están en ProductoLN.
    /// Los controles se crean por código en CrearControles().
    /// </summary>
    public partial class FrmProductos : Form
    {
        private readonly ProductoLN _productoLN = new ProductoLN();
        private Producto _productoSeleccionado;   // null = se está creando un producto nuevo

        private TextBox txtCodigo, txtNombre, txtPrecioCompra, txtPrecioVenta, txtStock, txtStockMinimo, txtBuscar;
        private ComboBox cboCategoria;
        private Button btnNuevo, btnGuardar, btnDesactivar;
        private DataGridView dgvProductos;
        private Label lblModo;

        public FrmProductos()
        {
            InitializeComponent();
            CrearControles();
            CargarProductos();
            Nuevo();
        }

        // ---------- Construcción de la interfaz ----------

        private void CrearControles()
        {
            Text = "Productos - Ferretería El Tornillo Feliz";
            ClientSize = new Size(980, 560);
            MinimumSize = new Size(900, 520);
            StartPosition = FormStartPosition.CenterParent;

            Icon icono = Logo.ObtenerIcono();
            if (icono != null) Icon = icono;

            // Panel izquierdo: datos del producto
            GroupBox gbDatos = new GroupBox
            {
                Text = "Datos del producto",
                Left = 12,
                Top = 12,
                Width = 330,
                Height = 345
            };

            lblModo = new Label { Left = 15, Top = 24, Width = 300, ForeColor = Color.DimGray };
            gbDatos.Controls.Add(lblModo);

            txtCodigo = AgregarCampo(gbDatos, "Código", 55);
            txtNombre = AgregarCampo(gbDatos, "Nombre", 90);

            gbDatos.Controls.Add(new Label { Text = "Categoría", Left = 15, Top = 129, Width = 120 });
            cboCategoria = new ComboBox { Left = 140, Top = 125, Width = 170, DropDownStyle = ComboBoxStyle.DropDown };
            cboCategoria.Items.AddRange(new object[] { "Herramientas", "Tornillería", "Pintura", "Eléctrico", "Plomería", "Otros" });
            gbDatos.Controls.Add(cboCategoria);

            txtPrecioCompra = AgregarCampo(gbDatos, "Precio de compra (Q)", 160);
            txtPrecioVenta = AgregarCampo(gbDatos, "Precio de venta (Q)", 195);
            txtStock = AgregarCampo(gbDatos, "Stock", 230);
            txtStockMinimo = AgregarCampo(gbDatos, "Stock mínimo", 265);

            Controls.Add(gbDatos);

            // Botones
            btnNuevo = new Button { Text = "Nuevo", Left = 12, Top = 372, Width = 100, Height = 34 };
            btnGuardar = new Button { Text = "Guardar", Left = 122, Top = 372, Width = 100, Height = 34 };
            btnDesactivar = new Button { Text = "Desactivar", Left = 232, Top = 372, Width = 110, Height = 34 };
            btnNuevo.Click += btnNuevo_Click;
            btnGuardar.Click += btnGuardar_Click;
            btnDesactivar.Click += btnDesactivar_Click;
            Controls.Add(btnNuevo);
            Controls.Add(btnGuardar);
            Controls.Add(btnDesactivar);

            // Panel derecho: búsqueda y lista
            Controls.Add(new Label { Text = "Buscar (código o nombre):", Left = 360, Top = 17, Width = 160 });
            txtBuscar = new TextBox { Left = 525, Top = 13, Width = 300 };
            txtBuscar.TextChanged += txtBuscar_TextChanged;
            Controls.Add(txtBuscar);

            dgvProductos = new DataGridView
            {
                Left = 360,
                Top = 45,
                Width = 608,
                Height = 470,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvProductos.Columns.Add(CrearColumna("Código", "Codigo", 14, null));
            dgvProductos.Columns.Add(CrearColumna("Nombre", "Nombre", 30, null));
            dgvProductos.Columns.Add(CrearColumna("Categoría", "Categoria", 16, null));
            dgvProductos.Columns.Add(CrearColumna("P. compra", "PrecioCompra", 14, "N2"));
            dgvProductos.Columns.Add(CrearColumna("P. venta", "PrecioVenta", 14, "N2"));
            dgvProductos.Columns.Add(CrearColumna("Stock", "Stock", 10, null));
            dgvProductos.Columns.Add(CrearColumna("Mínimo", "StockMinimo", 10, null));
            dgvProductos.CellClick += dgvProductos_CellClick;
            dgvProductos.DataBindingComplete += dgvProductos_DataBindingComplete;
            Controls.Add(dgvProductos);

            Controls.Add(new Label
            {
                Text = "Filas en rosa: stock en o bajo el mínimo.",
                Left = 360,
                Top = 525,
                Width = 400,
                ForeColor = Color.DimGray,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            });
            Tema.Aplicar(this);
        }

        private static TextBox AgregarCampo(Control contenedor, string etiqueta, int y)
        {
            contenedor.Controls.Add(new Label { Text = etiqueta, Left = 15, Top = y + 4, Width = 120 });
            TextBox caja = new TextBox { Left = 140, Top = y, Width = 170 };
            contenedor.Controls.Add(caja);
            return caja;
        }

        private static DataGridViewTextBoxColumn CrearColumna(string titulo, string propiedad, float peso, string formato)
        {
            DataGridViewTextBoxColumn columna = new DataGridViewTextBoxColumn
            {
                HeaderText = titulo,
                DataPropertyName = propiedad,
                FillWeight = peso
            };
            if (formato != null)
            {
                columna.DefaultCellStyle.Format = formato;
                columna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            return columna;
        }

        // ---------- Carga y estado del formulario ----------

        private void CargarProductos()
        {
            try
            {
                dgvProductos.DataSource = _productoLN.Buscar(txtBuscar.Text);
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void Nuevo()
        {
            _productoSeleccionado = null;
            txtCodigo.Clear();
            txtNombre.Clear();
            cboCategoria.Text = "";
            txtPrecioCompra.Clear();
            txtPrecioVenta.Clear();
            txtStock.Clear();
            txtStockMinimo.Clear();

            lblModo.Text = "Nuevo producto";
            btnGuardar.Text = "Guardar";
            btnDesactivar.Enabled = false;
            dgvProductos.ClearSelection();
            txtCodigo.Focus();
        }

        // ---------- Eventos ----------

        private void dgvProductos_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow fila in dgvProductos.Rows)
            {
                Producto p = fila.DataBoundItem as Producto;
                if (p != null && p.BajoMinimo)
                    fila.DefaultCellStyle.BackColor = Color.MistyRose;
            }
            dgvProductos.ClearSelection();
        }

        private void dgvProductos_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            Producto p = dgvProductos.Rows[e.RowIndex].DataBoundItem as Producto;
            if (p == null) return;

            _productoSeleccionado = p;
            txtCodigo.Text = p.Codigo;
            txtNombre.Text = p.Nombre;
            cboCategoria.Text = p.Categoria;
            txtPrecioCompra.Text = p.PrecioCompra.ToString("0.00", CultureInfo.InvariantCulture);
            txtPrecioVenta.Text = p.PrecioVenta.ToString("0.00", CultureInfo.InvariantCulture);
            txtStock.Text = p.Stock.ToString(CultureInfo.InvariantCulture);
            txtStockMinimo.Text = p.StockMinimo.ToString(CultureInfo.InvariantCulture);

            lblModo.Text = "Editando: " + p.Nombre;
            btnGuardar.Text = "Actualizar";
            btnDesactivar.Enabled = true;
        }

        private void txtBuscar_TextChanged(object sender, EventArgs e)
        {
            CargarProductos();
        }

        private void btnNuevo_Click(object sender, EventArgs e)
        {
            Nuevo();
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            try
            {
                Producto p = LeerFormulario();

                if (_productoSeleccionado == null)
                {
                    _productoLN.Registrar(p);
                    MessageBox.Show("Producto registrado correctamente.", "Productos",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    p.Id = _productoSeleccionado.Id;
                    _productoLN.Actualizar(p);
                    MessageBox.Show("Producto actualizado correctamente.", "Productos",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                CargarProductos();
                Nuevo();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void btnDesactivar_Click(object sender, EventArgs e)
        {
            if (_productoSeleccionado == null)
            {
                MessageBox.Show("Seleccione un producto de la lista.", "Productos",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult respuesta = MessageBox.Show(
                "¿Desactivar el producto \"" + _productoSeleccionado.Nombre + "\"?\n\n" +
                "Dejará de aparecer en el inventario, pero se conservará el historial de ventas.",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (respuesta != DialogResult.Yes) return;

            try
            {
                _productoLN.Desactivar(_productoSeleccionado.Id);
                CargarProductos();
                Nuevo();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        // ---------- Lectura y validación de formato de los campos ----------

        private Producto LeerFormulario()
        {
            return new Producto
            {
                Codigo = txtCodigo.Text.Trim(),
                Nombre = txtNombre.Text.Trim(),
                Categoria = cboCategoria.Text.Trim(),
                PrecioCompra = LeerDecimal(txtPrecioCompra, "Precio de compra"),
                PrecioVenta = LeerDecimal(txtPrecioVenta, "Precio de venta"),
                Stock = LeerEntero(txtStock, "Stock"),
                StockMinimo = LeerEntero(txtStockMinimo, "Stock mínimo"),
                Activo = true
            };
        }

        // La pantalla solo verifica que el texto sea un número; las reglas (negativos, límites) las valida la lógica.
        private static decimal LeerDecimal(TextBox caja, string campo)
        {
            decimal valor;
            string texto = caja.Text.Trim().Replace(',', '.');

            if (!decimal.TryParse(texto, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                                  CultureInfo.InvariantCulture, out valor))
                throw new ReglaNegocioException("El campo \"" + campo + "\" debe ser un número válido (ejemplo: 25.50).");

            return valor;
        }

        private static int LeerEntero(TextBox caja, string campo)
        {
            int valor;

            if (!int.TryParse(caja.Text.Trim(), NumberStyles.AllowLeadingSign,
                              CultureInfo.InvariantCulture, out valor))
                throw new ReglaNegocioException("El campo \"" + campo + "\" debe ser un número entero.");

            return valor;
        }

        private static void MostrarError(Exception ex)
        {
            if (ex is ReglaNegocioException)
            {
                MessageBox.Show(ex.Message, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("Ocurrió un error al acceder a los datos.\n\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
