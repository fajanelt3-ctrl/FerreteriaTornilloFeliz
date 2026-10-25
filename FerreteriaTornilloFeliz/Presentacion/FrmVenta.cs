using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using FerreteriaTornilloFeliz.Entidades;
using FerreteriaTornilloFeliz.LogicaNegocios;

namespace FerreteriaTornilloFeliz.Presentacion
{
    /// <summary>
    /// Pantalla de ventas: busca productos por código, arma el carrito y confirma la venta.
    /// Solo maneja la interfaz: la validación final y la transacción SQL están en VentaLN.
    /// Los controles se crean por código en CrearControles().
    /// </summary>
    public partial class FrmVenta : Form
    {
        private readonly ProductoLN _productoLN = new ProductoLN();
        private readonly VentaLN _ventaLN = new VentaLN();
        private Venta _venta = new Venta();           // carrito actual
        private Producto _productoActual;             // último producto encontrado

        private TextBox txtCodigo;
        private NumericUpDown nudCantidad;
        private Button btnBuscar, btnAgregar, btnQuitar, btnCancelar, btnConfirmar;
        private Label lblInfoProducto, lblTotal;
        private DataGridView dgvCarrito;

        public FrmVenta()
        {
            InitializeComponent();
            CrearControles();
            CargarCodigosSugeridos();
            RefrescarCarrito();
        }

        // ---------- Construcción de la interfaz ----------

        private void CrearControles()
        {
            Text = "Nueva venta - Ferretería El Tornillo Feliz";
            ClientSize = new Size(900, 560);
            MinimumSize = new Size(860, 540);
            StartPosition = FormStartPosition.CenterParent;

            Icon icono = Logo.ObtenerIcono();
            if (icono != null) Icon = icono;

            // Zona para agregar productos
            GroupBox gbAgregar = new GroupBox
            {
                Text = "Agregar producto",
                Left = 12,
                Top = 12,
                Width = 876,
                Height = 115,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            gbAgregar.Controls.Add(new Label { Text = "Código", Left = 15, Top = 31, Width = 55 });
            txtCodigo = new TextBox { Left = 75, Top = 27, Width = 140 };
            txtCodigo.KeyDown += txtCodigo_KeyDown;
            gbAgregar.Controls.Add(txtCodigo);

            btnBuscar = new Button { Text = "Buscar", Left = 225, Top = 25, Width = 80, Height = 28 };
            btnBuscar.Click += btnBuscar_Click;
            gbAgregar.Controls.Add(btnBuscar);

            gbAgregar.Controls.Add(new Label { Text = "Cantidad", Left = 330, Top = 31, Width = 60 });
            nudCantidad = new NumericUpDown { Left = 395, Top = 27, Width = 80, Minimum = 1, Maximum = 100000, Value = 1 };
            gbAgregar.Controls.Add(nudCantidad);

            btnAgregar = new Button { Text = "Agregar", Left = 490, Top = 25, Width = 100, Height = 28 };
            btnAgregar.Click += btnAgregar_Click;
            gbAgregar.Controls.Add(btnAgregar);

            lblInfoProducto = new Label
            {
                Left = 15,
                Top = 70,
                Width = 840,
                Height = 32,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            gbAgregar.Controls.Add(lblInfoProducto);

            Controls.Add(gbAgregar);

            // Carrito
            dgvCarrito = new DataGridView
            {
                Left = 12,
                Top = 140,
                Width = 876,
                Height = 305,
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
            dgvCarrito.Columns.Add(CrearColumna("Código", "CodigoProducto", 15, null));
            dgvCarrito.Columns.Add(CrearColumna("Producto", "NombreProducto", 40, null));
            dgvCarrito.Columns.Add(CrearColumna("Cantidad", "Cantidad", 12, null));
            dgvCarrito.Columns.Add(CrearColumna("Precio unit. (Q)", "PrecioUnitario", 16, "N2"));
            dgvCarrito.Columns.Add(CrearColumna("Subtotal (Q)", "Subtotal", 17, "N2"));
            Controls.Add(dgvCarrito);

            // Botones y total
            btnQuitar = new Button
            {
                Text = "Quitar seleccionado",
                Left = 12,
                Top = 458,
                Width = 160,
                Height = 34,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnQuitar.Click += btnQuitar_Click;
            Controls.Add(btnQuitar);

            btnCancelar = new Button
            {
                Text = "Cancelar venta",
                Left = 182,
                Top = 458,
                Width = 130,
                Height = 34,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnCancelar.Click += btnCancelar_Click;
            Controls.Add(btnCancelar);

            lblTotal = new Label
            {
                Left = 400,
                Top = 455,
                Width = 488,
                Height = 40,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            Controls.Add(lblTotal);

            btnConfirmar = new Button
            {
                Text = "Confirmar venta",
                Left = 688,
                Top = 505,
                Width = 200,
                Height = 40,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnConfirmar.Click += btnConfirmar_Click;
            Controls.Add(btnConfirmar);
            Tema.Aplicar(this);
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

        // Sugiere códigos mientras se escribe en la caja de código.
        private void CargarCodigosSugeridos()
        {
            try
            {
                AutoCompleteStringCollection codigos = new AutoCompleteStringCollection();
                foreach (Producto p in _productoLN.Listar())
                    codigos.Add(p.Codigo);

                txtCodigo.AutoCompleteCustomSource = codigos;
                txtCodigo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtCodigo.AutoCompleteSource = AutoCompleteSource.CustomSource;
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        // ---------- Lógica de la pantalla ----------

        // Busca el producto por código y muestra sus datos. Devuelve false si no se puede vender.
        private bool BuscarProducto()
        {
            _productoActual = null;
            lblInfoProducto.ForeColor = Color.Black;
            lblInfoProducto.Text = "";

            string codigo = txtCodigo.Text.Trim();
            if (codigo.Length == 0)
            {
                MessageBox.Show("Escriba el código del producto.", "Ventas",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            try
            {
                Producto p = _productoLN.ObtenerPorCodigo(codigo);

                if (p == null)
                {
                    lblInfoProducto.ForeColor = Color.Firebrick;
                    lblInfoProducto.Text = "No existe un producto activo con el código \"" + codigo + "\".";
                    return false;
                }

                _productoActual = p;
                lblInfoProducto.Text = p.Nombre + "   |   Precio: Q" + p.PrecioVenta.ToString("N2") +
                                       "   |   Existencias: " + p.Stock;

                if (p.Stock == 0)
                {
                    lblInfoProducto.ForeColor = Color.Firebrick;
                    lblInfoProducto.Text += "   (SIN EXISTENCIAS)";
                    return false;
                }

                if (p.BajoMinimo)
                    lblInfoProducto.ForeColor = Color.DarkOrange;

                return true;
            }
            catch (Exception ex)
            {
                MostrarError(ex);
                return false;
            }
        }

        private void RefrescarCarrito()
        {
            // Se asigna una copia para que la grilla se actualice al cambiar el carrito.
            dgvCarrito.DataSource = new List<DetalleVenta>(_venta.Detalles);
            dgvCarrito.ClearSelection();

            lblTotal.Text = "Total: Q" + _venta.CalcularTotal().ToString("N2");

            bool hayProductos = _venta.Detalles.Count > 0;
            btnConfirmar.Enabled = hayProductos;
            btnQuitar.Enabled = hayProductos;
            btnCancelar.Enabled = hayProductos;
        }

        private void PrepararNuevaLinea()
        {
            txtCodigo.Clear();
            nudCantidad.Value = 1;
            _productoActual = null;
            lblInfoProducto.Text = "";
            txtCodigo.Focus();
        }

        // ---------- Eventos ----------

        private void txtCodigo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BuscarProducto();
                nudCantidad.Focus();
            }
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            BuscarProducto();
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            // Siempre se consulta de nuevo para trabajar con el stock más reciente.
            if (!BuscarProducto())
                return;

            int cantidad = (int)nudCantidad.Value;

            int yaEnVenta = 0;
            foreach (DetalleVenta d in _venta.Detalles)
            {
                if (d.IdProducto == _productoActual.Id)
                    yaEnVenta = d.Cantidad;
            }

            if (cantidad + yaEnVenta > _productoActual.Stock)
            {
                MessageBox.Show(
                    "Solo hay " + _productoActual.Stock + " unidad(es) de \"" + _productoActual.Nombre +
                    "\" y ya tiene " + yaEnVenta + " en esta venta.",
                    "Stock insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _venta.AgregarDetalle(new DetalleVenta
            {
                IdProducto = _productoActual.Id,
                CodigoProducto = _productoActual.Codigo,
                NombreProducto = _productoActual.Nombre,
                Cantidad = cantidad,
                PrecioUnitario = _productoActual.PrecioVenta,
                CostoUnitario = _productoActual.PrecioCompra
            });

            RefrescarCarrito();
            PrepararNuevaLinea();
        }

        private void btnQuitar_Click(object sender, EventArgs e)
        {
            if (dgvCarrito.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una línea de la venta para quitarla.", "Ventas",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DetalleVenta linea = dgvCarrito.SelectedRows[0].DataBoundItem as DetalleVenta;
            if (linea == null) return;

            _venta.QuitarDetalle(linea.IdProducto);
            RefrescarCarrito();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            DialogResult respuesta = MessageBox.Show(
                "¿Cancelar la venta y vaciar la lista de productos?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (respuesta != DialogResult.Yes) return;

            _venta = new Venta();
            RefrescarCarrito();
            PrepararNuevaLinea();
        }

        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            if (_venta.Detalles.Count == 0)
            {
                MessageBox.Show("Agregue al menos un producto a la venta.", "Ventas",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult respuesta = MessageBox.Show(
                "¿Confirmar la venta por un total de Q" + _venta.CalcularTotal().ToString("N2") + "?",
                "Confirmar venta", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (respuesta != DialogResult.Yes) return;

            try
            {
                int idVenta = _ventaLN.RegistrarVenta(_venta);

                MessageBox.Show(
                    "Venta No. " + idVenta + " registrada correctamente.\nTotal: Q" + _venta.Total.ToString("N2"),
                    "Ventas", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _venta = new Venta();
                RefrescarCarrito();
                PrepararNuevaLinea();
            }
            catch (Exception ex)
            {
                // La venta NO se guardó (rollback). El carrito se conserva para corregirlo.
                MostrarError(ex);
            }
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
