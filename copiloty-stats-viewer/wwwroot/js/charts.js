window.copilotCharts = (function(){
  const charts = {};

  function ensureChart(id, type, data, options){
    const canvas = document.getElementById(id);
    if (!canvas) {
      console.warn(`Canvas element with id '${id}' not found`);
      return;
    }
    const ctx = canvas.getContext('2d');
    if(charts[id]){
      charts[id].data = data;
      charts[id].options = options || {};
      charts[id].update();
      return;
    }
    charts[id] = new Chart(ctx, { type, data, options: options || {} });
  }

  return {
    timeSeries: function(id, labels, interactions, generations, acceptances){
      const data = {
        labels,
        datasets: [
      { label: 'Interactions', data: interactions, borderColor: '#2b6c70', backgroundColor: 'rgba(43,108,112,0.18)', pointRadius: 0, tension: 0.25 },
      { label: 'Generations', data: generations, borderColor: '#005358', backgroundColor: 'rgba(0,83,88,0.2)', pointRadius: 0, tension: 0.25 },
      { label: 'Acceptances', data: acceptances, borderColor: '#7da8ab', backgroundColor: 'rgba(125,168,171,0.25)', pointRadius: 0, tension: 0.25 },
        ]
      };
    ensureChart(id, 'line', data, { responsive: true, interaction: { mode: 'index', intersect: false }, plugins: { legend: { position: 'bottom' }}, scales: { x: { grid: { display: false } }, y: { grid: { color: 'rgba(0,0,0,0.05)' } } } });
    },
    barMix: function(id, labels, counts, label){
    const data = { labels, datasets: [{ label, data: counts, backgroundColor: 'rgba(0,83,88,0.65)', borderColor: '#005358', borderWidth: 1, borderRadius: 4 }] };
    ensureChart(id, 'bar', data, { indexAxis: 'y', responsive: true, plugins: { legend: { display: false }}, scales: { x: { grid: { color: 'rgba(0,0,0,0.05)' } }, y: { grid: { display: false } } } });
    },
    pieMix: function(id, labels, counts){
    const base = ['#005358','#2b6c70','#4b8588','#7da8ab','#cfe2e3','#0b7d77','#3d8b8e','#9cc7c9','#1f6b6f','#74a4a6'];
    const colors = labels.map((_, i) => base[i % base.length]);
      const data = { labels, datasets: [{ data: counts, backgroundColor: colors }] };
    ensureChart(id, 'doughnut', data, { responsive: true, plugins: { legend: { position: 'right' }}, cutout: '60%' });
    }
  };
})();