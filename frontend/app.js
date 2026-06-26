const { useState, useEffect } = React;
const e = React.createElement;

const COLORS = ["Red", "Pink", "White", "Yellow"];
const SIZES = ["Small", "Medium", "Large"];
const MATERIALS = ["Glass", "Electronic", "Fabric", "Metal"];
const PACKAGINGS = ["Boxed", "Loose"];

const emptyProduct = {
  name: "", color: "Red", size: "Medium", price: "", discount: "0",
  material: "Glass", weightKg: "", fragile: false, containsLiquids: false,
  packaging: "Boxed", lengthCm: "", widthCm: "", heightCm: "",
};

function sel(name, value, options, onChange) {
  return e("select", { name, value, onChange: ev => onChange(ev.target.value) },
    ...options.map(o => e("option", { key: o, value: o }, o))
  );
}

function numInput(name, value, onChange, step) {
  return e("input", { name, type: "number", step: step || "0.01", min: "0", value, required: true,
    onChange: ev => onChange(ev.target.value) });
}

function fieldRow(labelText, input) {
  return e("label", null, labelText, input);
}

function ProductForm({ orderId, onAdded }) {
  const [form, setForm] = useState(emptyProduct);
  const [error, setError] = useState("");

  function set(field, value) { setForm(f => ({ ...f, [field]: value })); }

  async function submit(ev) {
    ev.preventDefault();
    setError("");
    const body = {
      name: form.name, color: form.color, size: form.size,
      price: parseFloat(form.price), discount: parseFloat(form.discount),
      material: form.material, weightKg: parseFloat(form.weightKg),
      fragile: form.fragile, containsLiquids: form.containsLiquids,
      packaging: form.packaging,
      dimensions: {
        lengthCm: parseFloat(form.lengthCm),
        widthCm: parseFloat(form.widthCm),
        heightCm: parseFloat(form.heightCm),
      },
    };
    const res = await fetch("/api/orders/" + orderId + "/products", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) { setError("Failed to add product."); return; }
    setForm(emptyProduct);
    onAdded(await res.json());
  }

  return e("form", { className: "product-form", onSubmit: submit, "data-testid": "product-form" },
    e("h4", null, "Add Product"),
    fieldRow("Name", e("input", { name: "name", value: form.name, required: true,
      onChange: ev => set("name", ev.target.value) })),
    fieldRow("Color", sel("color", form.color, COLORS, v => set("color", v))),
    fieldRow("Size", sel("size", form.size, SIZES, v => set("size", v))),
    fieldRow("Price (GBP)", numInput("price", form.price, v => set("price", v))),
    fieldRow("Discount (0-1)", numInput("discount", form.discount, v => set("discount", v))),
    fieldRow("Material", sel("material", form.material, MATERIALS, v => set("material", v))),
    fieldRow("Weight (kg)", numInput("weightKg", form.weightKg, v => set("weightKg", v))),
    e("label", { className: "checkbox-label" },
      e("input", { name: "fragile", type: "checkbox", checked: form.fragile,
        onChange: ev => set("fragile", ev.target.checked) }), "Fragile"),
    e("label", { className: "checkbox-label" },
      e("input", { name: "containsLiquids", type: "checkbox", checked: form.containsLiquids,
        onChange: ev => set("containsLiquids", ev.target.checked) }), "Contains Liquids"),
    fieldRow("Packaging", sel("packaging", form.packaging, PACKAGINGS, v => set("packaging", v))),
    e("fieldset", null,
      e("legend", null, "Dimensions (cm)"),
      fieldRow("Length", numInput("lengthCm", form.lengthCm, v => set("lengthCm", v), "0.1")),
      fieldRow("Width", numInput("widthCm", form.widthCm, v => set("widthCm", v), "0.1")),
      fieldRow("Height", numInput("heightCm", form.heightCm, v => set("heightCm", v), "0.1"))
    ),
    error ? e("p", { className: "error" }, error) : null,
    e("button", { type: "submit", "data-testid": "submit-product-btn" }, "Add Product")
  );
}

function OrderCard({ order, onUpdated }) {
  const [showForm, setShowForm] = useState(false);
  const [trackingInput, setTrackingInput] = useState("");
  const [showShipForm, setShowShipForm] = useState(false);

  function handleAdded(updated) { setShowForm(false); onUpdated(updated); }

  async function transition(endpoint, body) {
    const opts = { method: "POST" };
    if (body) { opts.headers = { "Content-Type": "application/json" }; opts.body = JSON.stringify(body); }
    const res = await fetch("/api/orders/" + order.id + "/" + endpoint, opts);
    if (res.ok) onUpdated(await res.json());
  }

  async function ship(ev) {
    ev.preventDefault();
    await transition("ship", { trackingNumber: trackingInput });
    setShowShipForm(false);
    setTrackingInput("");
  }

  const isPending   = order.status === "Pending";
  const isConfirmed = order.status === "Confirmed";
  const isShipped   = order.status === "Shipped";
  const isCancellable = order.status !== "Delivered" && order.status !== "Cancelled";
  const statusClass = "status-badge status-" + order.status.toLowerCase();

  return e("div", { className: "order-card", "data-testid": "order-card" },
    e("div", { className: "order-header" },
      e("span", { className: "order-id" }, "Order " + order.id.slice(0, 8) + "..."),
      e("span", { className: statusClass, "data-testid": "order-status" }, order.status),
      order.trackingNumber && e("span", { className: "tracking", "data-testid": "tracking-number" },
        "\uD83D\uDCE6 " + order.trackingNumber),
      e("span", { className: "order-total", "data-testid": "order-total" },
        "Total: \u00a3" + order.total.toFixed(2)),
      isPending && e("button", { "data-testid": "confirm-btn",
        onClick: function() { transition("confirm"); } }, "Confirm"),
      isPending && e("button", { "data-testid": "add-product-btn",
        onClick: function() { setShowForm(function(v) { return !v; }); } },
        showForm ? "Cancel" : "+ Add Product"),
      isConfirmed && e("button", { "data-testid": "ship-btn",
        onClick: function() { setShowShipForm(function(v) { return !v; }); } },
        showShipForm ? "Cancel" : "Ship"),
      isShipped && e("button", { "data-testid": "deliver-btn",
        onClick: function() { transition("deliver"); } }, "Mark Delivered"),
      isCancellable && e("button", { "data-testid": "cancel-btn", className: "btn-danger",
        onClick: function() { transition("cancel"); } }, "Cancel Order")
    ),
    isConfirmed && showShipForm && e("form", {
      className: "ship-form", onSubmit: ship, "data-testid": "ship-form"
    },
      e("input", { name: "trackingNumber", placeholder: "Tracking number", required: true,
        value: trackingInput, onChange: function(ev) { setTrackingInput(ev.target.value); } }),
      e("button", { type: "submit", "data-testid": "submit-ship-btn" }, "Confirm Shipment")
    ),
    order.products.length > 0
      ? e("table", { className: "product-table" },
          e("thead", null, e("tr", null,
            ["Name","Color","Size","Material","Price","Discount","Effective"].map(function(h) {
              return e("th", { key: h }, h);
            }))),
          e("tbody", null, order.products.map(function(p, i) {
            return e("tr", { key: i },
              e("td", null, p.name), e("td", null, p.color), e("td", null, p.size),
              e("td", null, p.material),
              e("td", null, "\u00a3" + p.price.toFixed(2)),
              e("td", null, (p.discount * 100).toFixed(0) + "%"),
              e("td", null, "\u00a3" + (p.price * (1 - p.discount)).toFixed(2))
            );
          }))
        )
      : null,
    showForm ? e(ProductForm, { orderId: order.id, onAdded: handleAdded }) : null
  );
}

function App() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(function() { loadOrders(); }, []);

  async function loadOrders() {
    const res = await fetch("/api/orders");
    if (res.ok) setOrders(await res.json());
    setLoading(false);
  }

  async function createOrder() {
    const res = await fetch("/api/orders", { method: "POST" });
    if (res.ok) {
      const order = await res.json();
      setOrders(function(prev) { return [order].concat(prev); });
    }
  }

  function updateOrder(updated) {
    setOrders(function(prev) { return prev.map(function(o) { return o.id === updated.id ? updated : o; }); });
  }

  return e("div", { className: "app" },
    e("header", null,
      e("h1", null, "Order Processor"),
      e("button", { "data-testid": "create-order-btn", onClick: createOrder }, "+ New Order")
    ),
    loading ? e("p", null, "Loading...") : null,
    e("div", { "data-testid": "order-list" },
      orders.length === 0 && !loading
        ? e("p", { className: "empty" }, "No orders yet. Create one above.")
        : null,
      orders.map(function(o) {
        return e(OrderCard, { key: o.id, order: o, onUpdated: updateOrder });
      })
    )
  );
}

ReactDOM.createRoot(document.getElementById("root")).render(e(App, null));
