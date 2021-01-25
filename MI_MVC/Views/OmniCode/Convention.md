<h3>OmniCode: rules for type inference from variable name</h3>

<p>OmniCode infers the type of a given variable based on its <u>name</u>. Here are the rules available since OmniCode 2.0:</p>

<table class="table table-bordered table-striped">
<tr>
	<th>Identifier rule</th>
	<th>Infered TIScript type</th>
</tr>

<tr>
	<td>'this' keyword</td>
	<td>always infered as Element type</td>
</tr>
<tr>
	<td>equals to 'gfx'</td>
	<td>Graphics</td>
</tr>
<tr>
	<td>equals to 'evt'</td>
	<td>Event</td>
</tr>
<tr>
	<td>equals to 'ds'</td>
	<td>DataSocket</td>
</tr>
<tr>
	<td>equals to 'ws'</td>
	<td>WebSocket</td>
</tr>
<tr>
	<td>equals to 'clr'</td>
	<td>Color</td>
</tr>
<tr>
	<td>equals to 'stream'</td>
	<td>Stream</td>
</tr>
<tr>
	<td>equals to 'proc'</td>
	<td>Process</td>
</tr>
<tr>
	<td>equals to 'task'</td>
	<td>Process</td>
</tr>
<tr>
	<td>equals to 'rq'</td>
	<td>Request</td>
</tr>
<tr>
	<td>equals to 'bytes'</td>
	<td>Bytes</td>
</tr>
<tr>
	<td>equals to 'gpath'</td>
	<td>Path</td>
</tr>
<tr>
	<td>equals to 'gtext'</td>
	<td>Text</td>
</tr>

<tr>
	<td>starts with ’el’ or contains ‘el_’</td>
	<td>Element</td>
</tr>
<tr>
	<td>starts with ’nd’ or contains ‘nd_’</td>
	<td>Node</td>
</tr>
<tr>
	<td>starts with ’fn’ or contains ‘fn_’</td>
	<td>Function</td>
</tr>
<tr>
	<td>starts with ’dt’ or contains ‘dt_’</td>
	<td>Date</td>
</tr>
<tr>
	<td>starts with ’arr’ or contains ‘arr_’</td>
	<td>Array</td>
</tr>
<tr>
	<td>starts with ’img’ or contains ‘img_’</td>
	<td>Image</td>
</tr>
<tr>
	<td>starts with ’obj’ or contains ‘obj_’</td>
	<td>Object</td>
</tr>
<tr>
	<td>starts with ’dic’ or contains ‘dic_’</td>
	<td>Object</td>
</tr>
<tr>
	<td>starts with ’prom’ or contains ‘prom_’</td>
	<td>promise</td>
</tr>
<tr>
	<td>ends with ‘path’</td>
	<td>String</td>
</tr>
<tr>
	<td>ends with ‘url’</td>
	<td>String</td>
</tr>
<tr>
	<td>equals to ‘name’</td>
	<td>String</td>
</tr>
<tr>
	<td>starts with ‘regx’ or equals to 're'</td>
	<td>RegExp</td>
</tr>
<tr>
	<td>contains ‘popup’</td>
	<td>View</td>
</tr>
<tr>
	<td>contains ‘wnd’</td>
	<td>View</td>
</tr>
</table>