using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using FerreteriaTornilloFeliz.Entidades;
using FerreteriaTornilloFeliz.LogicaNegocios;

namespace FerreteriaTornilloFeliz.Presentacion
{
    /// <summary>
    /// Ventana principal: muestra el logo, el menú de módulos y la alerta de productos
    /// con stock en el mínimo o por debajo. Los controles se crean por código.
    /// </summary>
    public partial class FrmPrincipal : Form
    {
        private readonly ProductoLN _productoLN = new ProductoLN();
        private DataGridView dgvAlertas;
        private Label lblResumenAlertas;

        public FrmPrincipal()
        {
            InitializeComponent();
            CrearControles();
            CargarAlertas();
        }

        private void CrearControles()
        {
            Text = "El Tornillo Feliz - Inventario y ventas";
            ClientSize = new Size(760, 500);
            MinimumSize = new Size(700, 450); 
            StartPosition = FormStartPosition.CenterScreen;

            Icon icono = Logo.ObtenerIcono();
            if (icono != null) Icon = icono;

            // Menú
            MenuStrip menu = new MenuStrip();
            ToolStripMenuItem mnuProductos = new ToolStripMenuItem("Productos");
            mnuProductos.Click += mnuProductos_Click;
            ToolStripMenuItem mnuVentas = new ToolStripMenuItem("Ventas");
            mnuVentas.Click += mnuVentas_Click;
            ToolStripMenuItem mnuReportes = new ToolStripMenuItem("Reportes");
            mnuReportes.Click += mnuReportes_Click;
            ToolStripMenuItem mnuSalir = new ToolStripMenuItem("Salir");
            mnuSalir.Click += delegate { Close(); };
            menu.Items.Add(mnuProductos);
            menu.Items.Add(mnuVentas);
            menu.Items.Add(mnuReportes);
            menu.Items.Add(mnuSalir);
            MainMenuStrip = menu;
            Controls.Add(menu);

            // Encabezado oscuro con logo
            Panel pnlEncabezado = new Panel
            {
                Left = 0,
                Top = 28,
                Width = ClientSize.Width,
                Height = 80,
                BackColor = Tema.Carbon,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlEncabezado.Controls.Add(new PictureBox
            {
                Left = 15,
                Top = 8,
                Width = 64,
                Height = 64,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = Logo.ObtenerImagen()
            });
            pnlEncabezado.Controls.Add(new Label
            {
                Text = "Ferretería El Tornillo Feliz",
                Left = 92,
                Top = 10,
                AutoSize = true,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Tema.Ambar
            });
            pnlEncabezado.Controls.Add(new Label
            {
                Text = "Sistema de inventario y ventas",
                Left = 95,
                Top = 48,
                AutoSize = true,
                ForeColor = Tema.Crema
            });
            Controls.Add(pnlEncabezado);

            // Alertas de stock
            GroupBox gbAlertas = new GroupBox
            {
                Text = "Alertas de stock",
                Left = 12,
                Top = 120,
                Width = 736,
                Height = 365,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            lblResumenAlertas = new Label { Left = 10, Top = 22, Width = 700, AutoSize = false, Height = 20 };
            gbAlertas.Controls.Add(lblResumenAlertas);

            dgvAlertas = new DataGridView
            {
                Left = 10,
                Top = 48,
                Width = 716,
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
            dgvAlertas.Columns.Add(CrearColumna("Código", "Codigo", 15));
            dgvAlertas.Columns.Add(CrearColumna("Producto", "Nombre", 40));
            dgvAlertas.Columns.Add(CrearColumna("Categoría", "Categoria", 20));
            dgvAlertas.Columns.Add(CrearColumna("Stock", "Stock", 12));
            dgvAlertas.Columns.Add(CrearColumna("Mínimo", "StockMinimo", 12));
            gbAlertas.Controls.Add(dgvAlertas);

            Controls.Add(gbAlertas);

            Tema.EstiloMenu(menu);
            Tema.Aplicar(this);
        }

        private static DataGridViewTextBoxColumn CrearColumna(string titulo, string propiedad, float peso)
        {
            return new DataGridViewTextBoxColumn
            {
                HeaderText = titulo,
                DataPropertyName = propiedad,
                FillWeight = peso
            };
        }

        private void CargarAlertas()
        {
            try
            {
                List<Producto> bajos = _productoLN.ListarBajoMinimo();
                dgvAlertas.DataSource = bajos;
                dgvAlertas.ClearSelection();

                lblResumenAlertas.Text = bajos.Count == 0
                    ? "Todos los productos tienen existencias suficientes."
                    : bajos.Count + " producto(s) en o bajo el stock mínimo. Conviene reabastecerlos.";
            }
            catch (Exception ex)
            {
                lblResumenAlertas.Text = "No se pudo consultar la base de datos: " + ex.Message;
            }
        }

        private void mnuProductos_Click(object sender, EventArgs e)
        {
            using (FrmProductos frm = new FrmProductos())
            {
                frm.ShowDialog(this);
            }
            CargarAlertas();
        }

        private void mnuVentas_Click(object sender, EventArgs e)
        {
            using (FrmVenta frm = new FrmVenta())
            {
                frm.ShowDialog(this);
            }
            CargarAlertas();
        }

        private void mnuReportes_Click(object sender, EventArgs e)
        {
            using (FrmReportes frm = new FrmReportes())
            {
                frm.ShowDialog(this);
            }
        }
    }
}
