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
    /// Pantalla de reportes: ventas por fechas, rentabilidad con punto de equilibrio y ventas por producto.
    /// Solo maneja la interfaz: las consultas y fórmulas están en VentaLN, ReporteLN y ConfiguracionLN.
    /// Los controles se crean por código en CrearControles().
    /// </summary>
    public partial class FrmReportes : Form
    {
        private readonly VentaLN _ventaLN = new VentaLN();
        private readonly ReporteLN _reporteLN = new ReporteLN();
        private readonly ConfiguracionLN _configuracionLN = new ConfiguracionLN();

        private DateTimePicker dtpDesde, dtpHasta;
        private Button btnConsultar, btnGuardarCostos;
        private TabControl tabs;

        private DataGridView dgvVentas, dgvProductos;
        private Label lblResumenVentas;

        private Label lblNumVentas, lblUnidades, lblIngreso, lblCosto, lblUtilidad, lblMargen;
        private Label lblEquilibrioUnidades, lblEquilibrioQuetzales;
        private TextBox txtCostosFijos;

        public FrmReportes()
        {
            InitializeComponent();
            CrearControles();
            CargarCostosFijos();
            Consultar();
        }

        // ---------- Construcción de la interfaz ----------

        private void CrearControles()
        {
            Text = "Reportes - Ferretería El Tornillo Feliz";
            ClientSize = new Size(900, 600);
            MinimumSize = new Size(860, 560);
            StartPosition = FormStartPosition.CenterParent;

            Icon icono = Logo.ObtenerIcono();
            if (icono != null) Icon = icono;

            // Selector de período
            Controls.Add(new Label { Text = "Desde", Left = 12, Top = 17, Width = 45 });
            dtpDesde = new DateTimePicker
            {
                Left = 60,
                Top = 13,
                Width = 125,
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            };
            Controls.Add(dtpDesde);

            Controls.Add(new Label { Text = "Hasta", Left = 205, Top = 17, Width = 45 });
            dtpHasta = new DateTimePicker
            {
                Left = 253,
                Top = 13,
                Width = 125,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            Controls.Add(dtpHasta);

            btnConsultar = new Button { Text = "Consultar", Left = 400, Top = 11, Width = 110, Height = 28 };
            btnConsultar.Click += btnConsultar_Click;
            Controls.Add(btnConsultar);

            // Pestañas
            tabs = new TabControl
            {
                Left = 12,
                Top = 50,
                Width = 876,
                Height = 538,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            TabPage tabVentas = new TabPage("Ventas");
            TabPage tabRentabilidad = new TabPage("Rentabilidad");
            TabPage tabProductos = new TabPage("Por producto");
            tabs.TabPages.Add(tabVentas);
            tabs.TabPages.Add(tabRentabilidad);
            tabs.TabPages.Add(tabProductos);
            Controls.Add(tabs);

            CrearPestanaVentas(tabVentas);
            CrearPestanaRentabilidad(tabRentabilidad);
            CrearPestanaProductos(tabProductos);
            Tema.Aplicar(this);
        }

        private void CrearPestanaVentas(TabPage tab)
        {
            dgvVentas = CrearGrilla();
            dgvVentas.Columns.Add(CrearColumna("No. de venta", "Id", 20, null));
            dgvVentas.Columns.Add(CrearColumna("Fecha", "Fecha", 40, "dd/MM/yyyy HH:mm"));
            dgvVentas.Columns.Add(CrearColumna("Total (Q)", "Total", 40, "N2"));

            lblResumenVentas = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            tab.Controls.Add(dgvVentas);
            tab.Controls.Add(lblResumenVentas);
            dgvVentas.BringToFront();   // la grilla (Fill) se acomoda al final, con el espacio que sobra
        }

        private void CrearPestanaRentabilidad(TabPage tab)
        {
            GroupBox gbResultados = new GroupBox
            {
                Text = "Resultados del período",
                Left = 8,
                Top = 8,
                Width = 430,
                Height = 215
            };
            lblNumVentas = AgregarDato(gbResultados, "Ventas realizadas", 28);
            lblUnidades = AgregarDato(gbResultados, "Unidades vendidas", 56);
            lblIngreso = AgregarDato(gbResultados, "Ingreso total", 84);
            lblCosto = AgregarDato(gbResultados, "Costo de lo vendido", 112);
            lblUtilidad = AgregarDato(gbResultados, "Utilidad", 140);
            lblMargen = AgregarDato(gbResultados, "Margen de utilidad", 168);
            tab.Controls.Add(gbResultados);

            GroupBox gbEquilibrio = new GroupBox
            {
                Text = "Punto de equilibrio",
                Left = 8,
                Top = 235,
                Width = 850,
                Height = 235
            };

            gbEquilibrio.Controls.Add(new Label { Text = "Costos fijos mensuales (Q)", Left = 15, Top = 34, Width = 200 });
            txtCostosFijos = new TextBox { Left = 220, Top = 30, Width = 120 };
            gbEquilibrio.Controls.Add(txtCostosFijos);

            btnGuardarCostos = new Button { Text = "Guardar", Left = 350, Top = 28, Width = 100, Height = 28 };
            btnGuardarCostos.Click += btnGuardarCostos_Click;
            gbEquilibrio.Controls.Add(btnGuardarCostos);

            lblEquilibrioUnidades = AgregarDato(gbEquilibrio, "Equilibrio en unidades", 72);
            lblEquilibrioQuetzales = AgregarDato(gbEquilibrio, "Equilibrio en ventas (Q)", 100);

            gbEquilibrio.Controls.Add(new Label
            {
                Left = 15,
                Top = 140,
                Width = 820,
                Height = 80,
                ForeColor = Color.DimGray,
                Text = "Punto de equilibrio = costos fijos ÷ utilidad promedio por unidad vendida en el período. " +
                       "Indica cuántas unidades (o cuántos quetzales en ventas) se necesitan para cubrir los costos fijos. " +
                       "Los costos fijos se consideran mensuales y el cálculo usa el margen real de las ventas del período consultado."
            });

            tab.Controls.Add(gbEquilibrio);
        }

        private void CrearPestanaProductos(TabPage tab)
        {
            dgvProductos = CrearGrilla();
            dgvProductos.Columns.Add(CrearColumna("Código", "Codigo", 14, null));
            dgvProductos.Columns.Add(CrearColumna("Producto", "Nombre", 32, null));
            dgvProductos.Columns.Add(CrearColumna("Unidades", "Unidades", 12, null));
            dgvProductos.Columns.Add(CrearColumna("Ingreso (Q)", "Ingreso", 14, "N2"));
            dgvProductos.Columns.Add(CrearColumna("Costo (Q)", "Costo", 14, "N2"));
            dgvProductos.Columns.Add(CrearColumna("Utilidad (Q)", "Utilidad", 14, "N2"));
            tab.Controls.Add(dgvProductos);
        }

        // Dentro de las pestañas se usa Dock = Fill (y no Anchor): así la grilla siempre ocupa
        // exactamente el área visible de la pestaña, sin importar el tamaño de la ventana.
        private static DataGridView CrearGrilla()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
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

        // Agrega una fila "etiqueta ........ valor" y devuelve la etiqueta del valor para actualizarla después.
        private static Label AgregarDato(Control contenedor, string etiqueta, int y)
        {
            contenedor.Controls.Add(new Label { Text = etiqueta, Left = 15, Top = y, Width = 200 });

            Label valor = new Label
            {
                Left = 220,
                Top = y - 3,
                Width = 190,
                Height = 24,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            contenedor.Controls.Add(valor);
            return valor;
        }

        // ---------- Carga de datos ----------

        private void CargarCostosFijos()
        {
            try
            {
                txtCostosFijos.Text = _configuracionLN.ObtenerCostosFijos().ToString("0.00", CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        // Consulta las tres pestañas con el mismo período.
        private void Consultar()
        {
            try
            {
                DateTime desde = dtpDesde.Value.Date;
                DateTime hasta = dtpHasta.Value.Date;

                CargarVentas(desde, hasta);
                CargarRentabilidad(desde, hasta);
                CargarProductos(desde, hasta);
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void CargarVentas(DateTime desde, DateTime hasta)
        {
            List<Venta> ventas = _ventaLN.ListarPorFechas(desde, hasta);
            dgvVentas.DataSource = ventas;
            dgvVentas.ClearSelection();

            decimal total = 0;
            foreach (Venta v in ventas)
                total += v.Total;

            lblResumenVentas.Text = ventas.Count == 0
                ? "No hay ventas en el período seleccionado."
                : ventas.Count + " venta(s) en el período. Total vendido: Q" + total.ToString("N2");
        }

        private void CargarRentabilidad(DateTime desde, DateTime hasta)
        {
            ResultadoRentabilidad r = _reporteLN.Rentabilidad(desde, hasta);

            lblNumVentas.Text = r.NumeroVentas.ToString("N0");
            lblUnidades.Text = r.UnidadesVendidas.ToString("N0");
            lblIngreso.Text = "Q" + r.Ingreso.ToString("N2");
            lblCosto.Text = "Q" + r.Costo.ToString("N2");
            lblUtilidad.Text = "Q" + r.Utilidad.ToString("N2");
            lblMargen.Text = r.MargenPorcentaje.ToString("N2") + " %";

            lblUtilidad.ForeColor = r.Utilidad < 0 ? Color.Firebrick : Color.DarkGreen;

            if (r.PuntoEquilibrioUnidades.HasValue && r.PuntoEquilibrioQuetzales.HasValue)
            {
                lblEquilibrioUnidades.Text = r.PuntoEquilibrioUnidades.Value.ToString("N0") + " unidades";
                lblEquilibrioQuetzales.Text = "Q" + r.PuntoEquilibrioQuetzales.Value.ToString("N2");
            }
            else
            {
                lblEquilibrioUnidades.Text = "No calculable";
                lblEquilibrioQuetzales.Text = "No calculable";
            }
        }

        private void CargarProductos(DateTime desde, DateTime hasta)
        {
            dgvProductos.DataSource = _reporteLN.MasVendidos(desde, hasta);
            dgvProductos.ClearSelection();
        }

        // ---------- Eventos ----------

        private void btnConsultar_Click(object sender, EventArgs e)
        {
            Consultar();
        }

        private void btnGuardarCostos_Click(object sender, EventArgs e)
        {
            try
            {
                decimal valor = LeerDecimal(txtCostosFijos, "Costos fijos mensuales");
                _configuracionLN.GuardarCostosFijos(valor);

                CargarCostosFijos();
                CargarRentabilidad(dtpDesde.Value.Date, dtpHasta.Value.Date);

                MessageBox.Show("Costos fijos guardados correctamente.", "Reportes",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        // La pantalla solo verifica que el texto sea un número; las reglas (negativos, límites) las valida la lógica.
        private static decimal LeerDecimal(TextBox caja, string campo)
        {
            decimal valor;
            string texto = caja.Text.Trim().Replace(',', '.');

            if (!decimal.TryParse(texto, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                                  CultureInfo.InvariantCulture, out valor))
                throw new ReglaNegocioException("El campo \"" + campo + "\" debe ser un número válido (ejemplo: 3000.00).");

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
